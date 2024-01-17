/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using NUnit.Framework;
using QuantConnect.Api;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using System;
using System.Collections;
using System.IO;
using System.Threading;

namespace QuantConnect.Tests.API
{
    /// <summary>
    /// Base test class for Api tests, provides the setup needed for all api tests
    /// </summary>
    public class ApiTestBase
    {
        internal int TestAccount;
        internal string TestToken;
        internal string TestOrganization;
        internal string DataFolder;
        internal Api.Api ApiClient;

        protected Project TestProject { get; private set; }
        protected Backtest TestBacktest { get; private set; }

        /// <summary>
        /// Run once before any RestApiTests
        /// </summary>
        [OneTimeSetUp]
        public void Setup()
        {
            ReloadConfiguration();

            TestAccount = Config.GetInt("job-user-id", 1);
            TestToken = Config.Get("api-access-token", "EnterTokenHere");
            TestOrganization = Config.Get("job-organization-id", "EnterOrgHere");
            DataFolder = Config.Get("data-folder");

            ApiClient = new Api.Api();
            ApiClient.Initialize(TestAccount, TestToken, DataFolder);

            // Let's create a project and backtest that can be used for general testing
            CreateTestProjectAndBacktest();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            DeleteTestProjectAndBacktest();
        }

        private void CreateTestProjectAndBacktest()
        {
            // Create a new project and backtest that can be used for testing
            Log.Debug("ApiTestBase.Setup(): Creating test project and backtest");
            var createProjectResult = ApiClient.CreateProject($"TestProject{DateTime.UtcNow.ToStringInvariant("yyyyMMddHHmmssfff")}",
                Language.CSharp, TestOrganization);
            if (!createProjectResult.Success)
            {
                Assert.Warn("Could not create test project, tests using it will fail.");
                return;
            }
            TestProject = createProjectResult.Projects[0];

            // Create a new compile for the project
            Log.Debug("ApiTestBase.Setup(): Creating test compile");
            var compile = ApiClient.CreateCompile(TestProject.ProjectId);
            if (!compile.Success)
            {
                Assert.Warn("Could not create compile for the test project, tests using it will fail.");
                return;
            }
            Log.Debug("ApiTestBase.Setup(): Waiting for test compile to complete");
            compile = WaitForCompilerResponse(TestProject.ProjectId, compile.CompileId);
            if (!compile.Success)
            {
                Assert.Warn("Could not create compile for the test project, tests using it will fail.");
                return;
            }

            // Create a backtest
            Log.Debug("ApiTestBase.Setup(): Creating test backtest");
            var backtestName = $"{DateTime.UtcNow.ToStringInvariant("u")} API Backtest";
            var backtest = ApiClient.CreateBacktest(TestProject.ProjectId, compile.CompileId, backtestName);
            if (!backtest.Success)
            {
                Assert.Warn("Could not create backtest for the test project, tests using it will fail.");
                return;
            }
            Log.Debug("ApiTestBase.Setup(): Waiting for test backtest to complete");
            TestBacktest = WaitForBacktestCompletion(TestProject.ProjectId, backtest.BacktestId);
            if (!TestBacktest.Success)
            {
                Assert.Warn("Could not create backtest for the test project, tests using it will fail.");
                return;
            }

            Log.Debug("ApiTestBase.Setup(): Test project and backtest created successfully");
        }

        private void DeleteTestProjectAndBacktest()
        {
            if (TestBacktest != null && TestBacktest.Success)
            {
                Log.Debug("ApiTestBase.TearDown(): Deleting test backtest");
                ApiClient.DeleteBacktest(TestProject.ProjectId, TestBacktest.BacktestId);
            }

            if (TestProject != null)
            {
                Log.Debug("ApiTestBase.TearDown(): Deleting test project");
                ApiClient.DeleteProject(TestProject.ProjectId);
            }
        }

        /// <summary>
        /// Wait for the compiler to respond to a specified compile request
        /// </summary>
        /// <param name="projectId">Id of the project</param>
        /// <param name="compileId">Id of the compilation of the project</param>
        /// <returns></returns>
        protected Compile WaitForCompilerResponse(int projectId, string compileId)
        {
            var compile = new Compile();
            var finish = DateTime.Now.AddSeconds(60);
            while (DateTime.Now < finish)
            {
                compile = ApiClient.ReadCompile(projectId, compileId);
                if (compile.State == CompileState.BuildSuccess) break;
                Thread.Sleep(1000);
            }
            return compile;
        }

        /// <summary>
        /// Wait for the backtest to complete
        /// </summary>
        /// <param name="projectId">Project id to scan</param>
        /// <param name="backtestId">Backtest id previously started</param>
        /// <returns>Completed backtest object</returns>
        protected Backtest WaitForBacktestCompletion(int projectId, string backtestId)
        {
            var result = new Backtest();
            var finish = DateTime.Now.AddSeconds(60);
            while (DateTime.Now < finish)
            {
                result = ApiClient.ReadBacktest(projectId, backtestId);
                if (result.Progress == 1) break;
                if (!result.Success) break;
                Thread.Sleep(1000);
            }
            return result;
        }

        /// <summary>
        /// Reload configuration, making sure environment variables are loaded into the config
        /// </summary>
        private static void ReloadConfiguration()
        {
            // nunit 3 sets the current folder to a temp folder we need it to be the test bin output folder
            var dir = TestContext.CurrentContext.TestDirectory;
            Environment.CurrentDirectory = dir;
            Directory.SetCurrentDirectory(dir);
            // reload config from current path
            Config.Reset();

            var environment = Environment.GetEnvironmentVariables();
            foreach (DictionaryEntry entry in environment)
            {
                var envKey = entry.Key.ToString();
                var value = entry.Value.ToString();

                if (envKey.StartsWith("QC_", StringComparison.InvariantCulture))
                {
                    var key = envKey.Substring(3).Replace("_", "-", StringComparison.InvariantCulture).ToLowerInvariant();
                    Log.Trace($"TestSetup(): Updating config setting '{key}' from environment var '{envKey}'");
                    Config.Set(key, value);
                }
            }

            // resets the version among other things
            Globals.Reset();
        }
    }
}
