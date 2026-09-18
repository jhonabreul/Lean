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

using Newtonsoft.Json.Linq;
using NUnit.Framework;
using QuantConnect.Api;

namespace QuantConnect.Tests.API
{
    /// <summary>
    /// Tests for the compile endpoints, run against a loopback stub so no api credentials are needed
    /// </summary>
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class CompileApiTests
    {
        private const string CreateCompileResponse = @"{
            ""compileId"": ""5d1f2cba3a0ec7407c566614300502b5-173e0419674daf4144ce7c9931155ca8"",
            ""state"": ""InQueue"",
            ""projectId"": 23456789,
            ""signature"": ""173e0419674daf4144ce7c9931155ca8"",
            ""signatureOrder"": [ ""main.py"" ],
            ""parameters"": [
                {
                    ""file"": ""main.py"",
                    ""parameters"": [
                        { ""line"": 8, ""type"": ""3 LEAN API parameters detected near \""self.set_start_date(2024, 1, 3)\"".."" }
                    ]
                }
            ],
            ""success"": true
        }";

        private const string ReadCompileResponse = @"{
            ""compileId"": ""5d1f2cba3a0ec7407c566614300502b5-173e0419674daf4144ce7c9931155ca8"",
            ""state"": ""BuildSuccess"",
            ""logs"": [ ""Build Request Successful"" ],
            ""success"": true
        }";

        [Test]
        public void CreateCompileSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(CreateCompileResponse);
            using var api = server.CreateApi();

            api.CreateCompile(23456789);

            var request = server.GetSingleRequest();
            Assert.AreEqual("/compile/create", request.Path);
            Assert.AreEqual(23456789, request.Body["projectId"].Value<int>());
        }

        [Test]
        public void CreateCompileExposesTheDetectedFileParameters()
        {
            using var server = new StubApiServer(CreateCompileResponse);
            using var api = server.CreateApi();

            var compile = api.CreateCompile(23456789);

            Assert.AreEqual(CompileState.InQueue, compile.State);
            Assert.AreEqual(1, compile.Parameters.Count);
            Assert.AreEqual("main.py", compile.Parameters[0].File);
            Assert.AreEqual(1, compile.Parameters[0].Parameters.Count);
            Assert.AreEqual(8, compile.Parameters[0].Parameters[0].Line);
            Assert.IsNotEmpty(compile.Parameters[0].Parameters[0].Type);
        }

        [Test]
        public void ReadCompileSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(ReadCompileResponse);
            using var api = server.CreateApi();

            var compile = api.ReadCompile(23456789, "5d1f2cba3a0ec7407c566614300502b5-173e0419674daf4144ce7c9931155ca8");

            var request = server.GetSingleRequest();
            Assert.AreEqual("/compile/read", request.Path);
            Assert.AreEqual(23456789, request.Body["projectId"].Value<int>());
            Assert.AreEqual("5d1f2cba3a0ec7407c566614300502b5-173e0419674daf4144ce7c9931155ca8",
                request.Body["compileId"].Value<string>());
            Assert.AreEqual(CompileState.BuildSuccess, compile.State);
            Assert.AreEqual(1, compile.Logs.Count);
        }
    }
}
