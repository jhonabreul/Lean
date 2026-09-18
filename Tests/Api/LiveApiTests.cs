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

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace QuantConnect.Tests.API
{
    /// <summary>
    /// Tests for the live management endpoints, run against a loopback stub so no api credentials are needed
    /// </summary>
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class LiveApiTests
    {
        private const string Auth0Response = @"{
            ""authorization"": { ""alpaca-access-token"": ""37a2c4f3-7b1c-41a4-b103-197a88ef2a4d"" },
            ""success"": true
        }";

        private const string LiveListResponse = @"{
            ""live"": [
                {
                    ""projectId"": 23456789,
                    ""deployId"": ""L-6e9d8a78f5af89d401f630585be90e43"",
                    ""status"": ""Running"",
                    ""launched"": ""2024-06-07T20:00:00Z"",
                    ""stopped"": null,
                    ""brokerage"": ""QuantConnectBrokerage"",
                    ""subscription"": ""Strategy Equity"",
                    ""equity"": 100250.75,
                    ""environment"": ""live-paper"",
                    ""description"": ""My live project"",
                    ""error"": """",
                    ""leagues"": [ ""Alpha"", ""Beta"" ]
                }
            ],
            ""success"": true
        }";

        private const string LiveAlgorithmResponse = @"{
            ""message"": """",
            ""deployId"": ""L-6e9d8a78f5af89d401f630585be90e43"",
            ""status"": ""Running"",
            ""cloneId"": 24058693,
            ""launched"": ""2024-06-07T20:00:00Z"",
            ""stopped"": null,
            ""brokerage"": ""QuantConnectBrokerage"",
            ""securityTypes"": ""Equity"",
            ""datacenter"": ""NY7"",
            ""isPublicStreaming"": true,
            ""public"": false,
            ""description"": ""My live project"",
            ""projectName"": ""My live project name"",
            ""files"": [],
            ""runtimeStatistics"": { ""Equity"": ""$100.00"" },
            ""success"": true
        }";

        private const string CreateLiveAlgorithmResponse = @"{
            ""responseCode"": ""200"",
            ""source"": ""live"",
            ""deployId"": ""L-141106d80de1da9a9f85ea07c06bf7b6"",
            ""versionId"": 17202,
            ""projectId"": 24058693,
            ""live"": {
                ""deployId"": ""L-141106d80de1da9a9f85ea07c06bf7b6"",
                ""status"": ""Initializing"",
                ""brokerage"": ""QuantConnectBrokerage"",
                ""projectName"": ""My live project name"",
                ""success"": true
            },
            ""success"": true
        }";

        [Test]
        public void ReadLiveAuth0SendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(Auth0Response);
            using var api = server.CreateApi();

            var response = api.ReadLiveAuth0("alpaca");

            var request = server.GetSingleRequest();
            Assert.AreEqual("/live/auth0/read", request.Path);
            Assert.AreEqual("alpaca", request.Body["brokerage"].Value<string>());
            Assert.IsTrue(response.Success);
            Assert.AreEqual("37a2c4f3-7b1c-41a4-b103-197a88ef2a4d", response.Authorization["alpaca-access-token"].ToString());
        }

        [Test]
        public void ListLiveAlgorithmsSendsTheDocumentedFilters()
        {
            using var server = new StubApiServer(LiveListResponse);
            using var api = server.CreateApi();

            api.ListLiveAlgorithms(AlgorithmStatus.Running, 23456789);

            var request = server.GetSingleRequest();
            Assert.AreEqual("/live/list", request.Path);
            Assert.AreEqual("Running", request.Body["status"].Value<string>());
            Assert.AreEqual(23456789, request.Body["projectId"].Value<int>());
        }

        [Test]
        public void ListLiveAlgorithmsOmitsTheProjectIdWhenNotProvided()
        {
            using var server = new StubApiServer(LiveListResponse);
            using var api = server.CreateApi();

            api.ListLiveAlgorithms(AlgorithmStatus.Running);

            Assert.IsNull(server.GetSingleRequest().Body["projectId"]);
        }

        [Test]
        public void ListLiveAlgorithmsDeserializesTheDocumentedSummaryFields()
        {
            using var server = new StubApiServer(LiveListResponse);
            using var api = server.CreateApi();

            var algorithm = api.ListLiveAlgorithms().Algorithms[0];

            Assert.AreEqual(23456789, algorithm.ProjectId);
            Assert.AreEqual("L-6e9d8a78f5af89d401f630585be90e43", algorithm.DeployId);
            Assert.AreEqual(AlgorithmStatus.Running, algorithm.Status);
            Assert.AreEqual("Strategy Equity", algorithm.Subscription);
            Assert.AreEqual(100250.75m, algorithm.Equity);
            Assert.AreEqual("live-paper", algorithm.Environment);
            Assert.AreEqual("My live project", algorithm.Description);
            Assert.AreEqual(new List<string> { "Alpha", "Beta" }, algorithm.Leagues);
        }

        [Test]
        public void ReadLiveAlgorithmOmitsTheDeployIdWhenNotProvided()
        {
            using var server = new StubApiServer(LiveAlgorithmResponse);
            using var api = server.CreateApi();

            api.ReadLiveAlgorithm(23456789);

            var request = server.GetSingleRequest();
            Assert.AreEqual("/live/read", request.Path);
            Assert.AreEqual(23456789, request.Body["projectId"].Value<int>());
            Assert.IsNull(request.Body["deployId"]);
        }

        [Test]
        public void ReadLiveAlgorithmSendsTheDeployIdWhenProvided()
        {
            using var server = new StubApiServer(LiveAlgorithmResponse);
            using var api = server.CreateApi();

            api.ReadLiveAlgorithm(23456789, "L-6e9d8a78f5af89d401f630585be90e43");

            Assert.AreEqual("L-6e9d8a78f5af89d401f630585be90e43",
                server.GetSingleRequest().Body["deployId"].Value<string>());
        }

        [Test]
        public void ReadLiveAlgorithmDeserializesTheDocumentedResponse()
        {
            using var server = new StubApiServer(LiveAlgorithmResponse);
            using var api = server.CreateApi();

            var response = api.ReadLiveAlgorithm(23456789);

            Assert.IsTrue(response.Success);
            Assert.AreEqual("Running", response.Status);
            Assert.AreEqual(24058693, response.CloneId);
            Assert.AreEqual(new DateTime(2024, 06, 07, 20, 0, 0, DateTimeKind.Utc), response.Launched.ToUniversalTime());
            Assert.IsNull(response.Stopped);
            Assert.AreEqual("NY7", response.Datacenter);
            Assert.AreEqual("My live project", response.Description);
            Assert.IsTrue(response.IsPublicStreaming);
            Assert.AreEqual("$100.00", response.RuntimeStatistics["Equity"]);
        }

        [Test]
        public void ReadLiveAlgorithmDeserializesTheDocumentedErrors()
        {
            using var server = new StubApiServer(@"{ ""success"": false, ""errors"": [ ""Project not found"", ""No live deployment"" ] }");
            using var api = server.CreateApi();

            var response = api.ReadLiveAlgorithm(23456789);

            Assert.IsFalse(response.Success);
            Assert.AreEqual(new[] { "Project not found", "No live deployment" }, response.Errors);
        }

        [Test]
        public void CreateLiveAlgorithmSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(CreateLiveAlgorithmResponse);
            using var api = server.CreateApi();

            api.CreateLiveAlgorithm(24058693, "5d1f2cba3a0ec7407c566614300502b5-173e0419674daf4144ce7c9931155ca8",
                "LN-c54129e1b4f667613d3f34542b787771",
                new Dictionary<string, object> { { "id", "QuantConnectBrokerage" } });

            var request = server.GetSingleRequest();
            Assert.AreEqual("/live/create", request.Path);
            Assert.AreEqual(24058693, request.Body["projectId"].Value<int>());
            Assert.AreEqual("5d1f2cba3a0ec7407c566614300502b5-173e0419674daf4144ce7c9931155ca8", request.Body["compileId"].Value<string>());
            Assert.AreEqual("LN-c54129e1b4f667613d3f34542b787771", request.Body["nodeId"].Value<string>());
            Assert.AreEqual("-1", request.Body["versionId"].Value<string>());
            Assert.AreEqual("QuantConnectBrokerage", request.Body["brokerage"]["id"].Value<string>());
            Assert.IsNotNull(request.Body["dataProviders"]);
        }

        [Test]
        public void CreateLiveAlgorithmExposesTheDeployedAlgorithm()
        {
            using var server = new StubApiServer(CreateLiveAlgorithmResponse);
            using var api = server.CreateApi();

            var response = api.CreateLiveAlgorithm(24058693, "5d1f2cba3a0ec7407c566614300502b5-173e0419674daf4144ce7c9931155ca8",
                "LN-c54129e1b4f667613d3f34542b787771",
                new Dictionary<string, object> { { "id", "QuantConnectBrokerage" } });

            Assert.AreEqual("L-141106d80de1da9a9f85ea07c06bf7b6", response.DeployId);
            Assert.AreEqual(17202, response.VersionId);
            Assert.IsNotNull(response.Live);
            Assert.AreEqual("L-141106d80de1da9a9f85ea07c06bf7b6", response.Live.DeployId);
            Assert.AreEqual("Initializing", response.Live.Status);
            Assert.AreEqual("My live project name", response.Live.ProjectName);
        }

        [Test]
        public void ReadLiveInsightsSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(@"{ ""insights"": [], ""length"": 0, ""success"": true }");
            using var api = server.CreateApi();

            api.ReadLiveInsights(23456789, "L-6e9d8a78f5af89d401f630585be90e43", 10, 60);

            var request = server.GetSingleRequest();
            Assert.AreEqual("/live/insights/read", request.Path);
            Assert.AreEqual(23456789, request.Body["projectId"].Value<int>());
            Assert.AreEqual("L-6e9d8a78f5af89d401f630585be90e43", request.Body["algorithmId"].Value<string>());
            Assert.AreEqual(10, request.Body["start"].Value<int>());
            Assert.AreEqual(60, request.Body["end"].Value<int>());
        }

        [Test]
        public void ReadLiveInsightsOmitsTheAlgorithmIdWhenNotProvided()
        {
            using var server = new StubApiServer(@"{ ""insights"": [], ""length"": 0, ""success"": true }");
            using var api = server.CreateApi();

            api.ReadLiveInsights(23456789);

            Assert.IsNull(server.GetSingleRequest().Body["algorithmId"]);
        }

        [Test]
        public void ReadLiveOrdersExposesTheLoadingResponse()
        {
            using var server = new StubApiServer(@"{ ""progress"": 0.25, ""status"": ""loading"", ""success"": true }");
            using var api = server.CreateApi();

            var response = api.ReadLiveOrders(23456789);

            Assert.AreEqual("loading", response.Status);
            Assert.AreEqual(0.25m, response.Progress);
            Assert.IsEmpty(response.Orders);
        }
    }
}
