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
    /// Tests for the project and collaboration endpoints, run against a loopback stub so no api credentials are needed
    /// </summary>
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class ProjectsApiTests
    {
        private const string ProjectListResponse = @"{
            ""projects"": [
                {
                    ""projectId"": 23456789,
                    ""organizationId"": ""5cad178b20a1d52567b534553413b691"",
                    ""name"": ""My Project"",
                    ""created"": ""2024-01-03T00:00:00Z"",
                    ""modified"": ""2024-01-04T00:00:00Z"",
                    ""ownerId"": 1234,
                    ""language"": ""Py"",
                    ""leanVersionId"": 11157,
                    ""leanPinnedToMaster"": true,
                    ""owner"": true,
                    ""description"": ""A description"",
                    ""channelId"": ""channel"",
                    ""isPinned"": true,
                    ""maxFileSize"": 32768,
                    ""sharingTokenBacktest"": ""q4eoFHGHDxdpx7CSsrhTaMQmLSArOjOPJppFryHAVMWsqXyRxZGKjkOyZ5zj5Lqo""
                }
            ],
            ""versions"": [ { ""id"": 11157, ""leanHash"": ""abc123"", ""public"": true } ],
            ""success"": true
        }";

        private const string CollaboratorsResponse = @"{
            ""collaborators"": [
                {
                    ""uid"": 1234,
                    ""liveControl"": true,
                    ""permission"": ""write"",
                    ""publicId"": ""mia-ai"",
                    ""profileImage"": ""https://cdn.quantconnect.com/web/i/users/profile/abc123.jpeg"",
                    ""email"": ""abc@123.com"",
                    ""name"": ""Mia"",
                    ""bio"": ""A bio"",
                    ""owner"": false
                }
            ],
            ""userLiveControl"": true,
            ""userPermissions"": ""write"",
            ""success"": true
        }";

        private const string SuccessResponse = @"{ ""success"": true, ""errors"": [] }";

        [Test]
        public void ListProjectsSendsTheDocumentedPagingRequest()
        {
            using var server = new StubApiServer(ProjectListResponse);
            using var api = server.CreateApi();

            api.ListProjects(10, 60);

            var request = server.GetSingleRequest();
            Assert.AreEqual("/projects/read", request.Path);
            Assert.AreEqual(10, request.Body["start"].Value<int>());
            Assert.AreEqual(60, request.Body["end"].Value<int>());
            Assert.IsNull(request.Body["projectId"]);
        }

        [Test]
        public void ListProjectsOmitsTheEndIndexWhenNotProvided()
        {
            using var server = new StubApiServer(ProjectListResponse);
            using var api = server.CreateApi();

            api.ListProjects();

            var request = server.GetSingleRequest();
            Assert.AreEqual(0, request.Body["start"].Value<int>());
            Assert.IsNull(request.Body["end"]);
        }

        [Test]
        public void ReadProjectDeserializesTheDocumentedProjectFields()
        {
            using var server = new StubApiServer(ProjectListResponse);
            using var api = server.CreateApi();

            var project = api.ReadProject(23456789).Projects[0];

            Assert.AreEqual(23456789, project.ProjectId);
            Assert.AreEqual(Language.Python, project.Language);
            Assert.IsTrue(project.IsPinned);
            Assert.AreEqual(32768, project.MaxFileSize);
            Assert.AreEqual("q4eoFHGHDxdpx7CSsrhTaMQmLSArOjOPJppFryHAVMWsqXyRxZGKjkOyZ5zj5Lqo", project.SharingTokenBacktest);
        }

        [Test]
        public void UpdateProjectSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(SuccessResponse);
            using var api = server.CreateApi();

            api.UpdateProject(23456789, "New Project Name", "New Project Description");

            var request = server.GetSingleRequest();
            Assert.AreEqual("/projects/update", request.Path);
            Assert.AreEqual(23456789, request.Body["projectId"].Value<int>());
            Assert.AreEqual("New Project Name", request.Body["name"].Value<string>());
            Assert.AreEqual("New Project Description", request.Body["description"].Value<string>());
        }

        [Test]
        public void UpdateProjectOmitsThePropertiesNotProvided()
        {
            using var server = new StubApiServer(SuccessResponse);
            using var api = server.CreateApi();

            api.UpdateProject(23456789, description: "New Project Description");

            var request = server.GetSingleRequest();
            Assert.IsNull(request.Body["name"]);
            Assert.AreEqual("New Project Description", request.Body["description"].Value<string>());
        }

        [Test]
        public void CreateProjectCollaboratorSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(CollaboratorsResponse);
            using var api = server.CreateApi();

            var response = api.CreateProjectCollaborator(23456789, "mia-ai", true, false);

            var request = server.GetSingleRequest();
            Assert.AreEqual("/projects/collaboration/create", request.Path);
            Assert.AreEqual(23456789, request.Body["projectId"].Value<int>());
            Assert.AreEqual("mia-ai", request.Body["collaboratorUserId"].Value<string>());
            Assert.IsTrue(request.Body["collaborationLiveControl"].Value<bool>());
            Assert.IsFalse(request.Body["collaborationWrite"].Value<bool>());
            Assert.AreEqual(1, response.Collaborators.Count);
            Assert.AreEqual("abc@123.com", response.Collaborators[0].Email);
        }

        [Test]
        public void ReadProjectCollaboratorsExposesTheOwnerPermissions()
        {
            using var server = new StubApiServer(CollaboratorsResponse);
            using var api = server.CreateApi();

            var response = api.ReadProjectCollaborators(23456789);

            var request = server.GetSingleRequest();
            Assert.AreEqual("/projects/collaboration/read", request.Path);
            Assert.AreEqual(23456789, request.Body["projectId"].Value<int>());
            Assert.IsTrue(response.UserLiveControl);
            Assert.AreEqual("write", response.UserPermissions);
            Assert.AreEqual(1, response.Collaborators.Count);
        }

        [Test]
        public void UpdateProjectCollaboratorSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(CollaboratorsResponse);
            using var api = server.CreateApi();

            api.UpdateProjectCollaborator(23456789, "mia-ai", true, true);

            var request = server.GetSingleRequest();
            Assert.AreEqual("/projects/collaboration/update", request.Path);
            Assert.AreEqual("mia-ai", request.Body["collaboratorUserId"].Value<string>());
            Assert.IsTrue(request.Body["liveControl"].Value<bool>());
            Assert.IsTrue(request.Body["write"].Value<bool>());
        }

        [Test]
        public void DeleteProjectCollaboratorSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(CollaboratorsResponse);
            using var api = server.CreateApi();

            api.DeleteProjectCollaborator(23456789, "mia-ai");

            var request = server.GetSingleRequest();
            Assert.AreEqual("/projects/collaboration/delete", request.Path);
            Assert.AreEqual(23456789, request.Body["projectId"].Value<int>());
            Assert.AreEqual("mia-ai", request.Body["collaboratorId"].Value<string>());
        }

        [Test]
        public void AcquireProjectCollaborationLockSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(SuccessResponse);
            using var api = server.CreateApi();

            var response = api.AcquireProjectCollaborationLock(23456789, "MCP Server");

            var request = server.GetSingleRequest();
            Assert.AreEqual("/projects/collaboration/lock/acquire", request.Path);
            Assert.AreEqual(23456789, request.Body["projectId"].Value<int>());
            Assert.AreEqual("MCP Server", request.Body["codeSourceId"].Value<string>());
            Assert.IsTrue(response.Success);
        }
    }
}
