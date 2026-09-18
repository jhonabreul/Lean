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

namespace QuantConnect.Tests.API
{
    /// <summary>
    /// Tests for the project file endpoints, run against a loopback stub so no api credentials are needed
    /// </summary>
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class FilesApiTests
    {
        private const string ProjectFilesResponse = @"{
            ""files"": [
                {
                    ""id"": 1,
                    ""projectId"": 23456789,
                    ""name"": ""main.py"",
                    ""content"": ""a = 2"",
                    ""modified"": ""2024-01-03T00:00:00Z"",
                    ""open"": true,
                    ""isLibrary"": false
                }
            ],
            ""success"": true
        }";

        private const string SuccessResponse = @"{ ""success"": true, ""errors"": [] }";

        private const string Patch = @"diff --git a/main.py b/main.py
index 5a38b08..72c8d1e 100644
--- a/main.py
+++ b/main.py
@@ -2,4 +2,4 @@
-a = 1
+a = 2
";

        [Test]
        public void AddProjectFileSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(SuccessResponse);
            using var api = server.CreateApi();

            api.AddProjectFile(23456789, "main.py", "a = 1", "MCP Server");

            var request = server.GetSingleRequest();
            Assert.AreEqual("/files/create", request.Path);
            Assert.AreEqual(23456789, request.Body["projectId"].Value<int>());
            Assert.AreEqual("main.py", request.Body["name"].Value<string>());
            Assert.AreEqual("a = 1", request.Body["content"].Value<string>());
            Assert.AreEqual("MCP Server", request.Body["codeSourceId"].Value<string>());
        }

        [Test]
        public void AddProjectFileOmitsTheCodeSourceIdWhenNotProvided()
        {
            using var server = new StubApiServer(SuccessResponse);
            using var api = server.CreateApi();

            api.AddProjectFile(23456789, "main.py", "a = 1");

            Assert.IsNull(server.GetSingleRequest().Body["codeSourceId"]);
        }

        [Test]
        public void UpdateProjectFileNameSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(SuccessResponse);
            using var api = server.CreateApi();

            api.UpdateProjectFileName(23456789, "file1.py", "file2.py", "MCP Server");

            var request = server.GetSingleRequest();
            Assert.AreEqual("/files/update", request.Path);
            Assert.AreEqual("file1.py", request.Body["name"].Value<string>());
            Assert.AreEqual("file2.py", request.Body["newName"].Value<string>());
            Assert.AreEqual("MCP Server", request.Body["codeSourceId"].Value<string>());
        }

        [Test]
        public void UpdateProjectFileContentSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(ProjectFilesResponse);
            using var api = server.CreateApi();

            api.UpdateProjectFileContent(23456789, "main.py", "a = 2", "MCP Server");

            var request = server.GetSingleRequest();
            Assert.AreEqual("/files/update", request.Path);
            Assert.AreEqual("main.py", request.Body["name"].Value<string>());
            Assert.AreEqual("a = 2", request.Body["content"].Value<string>());
            Assert.AreEqual("MCP Server", request.Body["codeSourceId"].Value<string>());
        }

        [Test]
        public void UpdateProjectFileContentExposesTheUpdatedFiles()
        {
            using var server = new StubApiServer(ProjectFilesResponse);
            using var api = server.CreateApi();

            var response = api.UpdateProjectFileContent(23456789, "main.py", "a = 2");

            Assert.IsTrue(response.Success);
            Assert.AreEqual(1, response.Files.Count);
            Assert.AreEqual("main.py", response.Files[0].Name);
            Assert.AreEqual("a = 2", response.Files[0].Code);
        }

        [Test]
        public void ReadProjectFileSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(ProjectFilesResponse);
            using var api = server.CreateApi();

            api.ReadProjectFile(23456789, "main.py", "MCP Server");

            var request = server.GetSingleRequest();
            Assert.AreEqual("/files/read", request.Path);
            Assert.AreEqual("main.py", request.Body["name"].Value<string>());
            Assert.AreEqual("MCP Server", request.Body["codeSourceId"].Value<string>());
        }

        [Test]
        public void ReadProjectFilesSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(ProjectFilesResponse);
            using var api = server.CreateApi();

            api.ReadProjectFiles(23456789, "MCP Server");

            var request = server.GetSingleRequest();
            Assert.AreEqual("/files/read", request.Path);
            Assert.IsNull(request.Body["name"]);
            Assert.AreEqual("MCP Server", request.Body["codeSourceId"].Value<string>());
        }

        [Test]
        public void DeleteProjectFileSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(SuccessResponse);
            using var api = server.CreateApi();

            api.DeleteProjectFile(23456789, "main.py", "MCP Server");

            var request = server.GetSingleRequest();
            Assert.AreEqual("/files/delete", request.Path);
            Assert.AreEqual("main.py", request.Body["name"].Value<string>());
            Assert.AreEqual("MCP Server", request.Body["codeSourceId"].Value<string>());
        }

        [Test]
        public void PatchProjectFileSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(SuccessResponse);
            using var api = server.CreateApi();

            var response = api.PatchProjectFile(23456789, Patch, "MCP Server");

            var request = server.GetSingleRequest();
            Assert.AreEqual("/files/patch", request.Path);
            Assert.AreEqual(23456789, request.Body["projectId"].Value<int>());
            Assert.AreEqual(Patch, request.Body["patch"].Value<string>());
            Assert.AreEqual("MCP Server", request.Body["codeSourceId"].Value<string>());
            Assert.IsTrue(response.Success);
        }

        [Test]
        public void PatchProjectFileOmitsTheCodeSourceIdWhenNotProvided()
        {
            using var server = new StubApiServer(SuccessResponse);
            using var api = server.CreateApi();

            api.PatchProjectFile(23456789, Patch);

            Assert.IsNull(server.GetSingleRequest().Body["codeSourceId"]);
        }
    }
}
