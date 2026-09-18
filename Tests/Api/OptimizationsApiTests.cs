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

using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using QuantConnect.Optimizer;
using QuantConnect.Optimizer.Objectives;
using QuantConnect.Optimizer.Parameters;
using QuantConnect.Util;

namespace QuantConnect.Tests.API
{
    /// <summary>
    /// Tests for the optimization endpoints, run against a loopback stub so no api credentials are needed
    /// </summary>
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class OptimizationsApiTests
    {
        private const string ListOptimizationResponse = @"{
            ""optimizations"": [
                {
                    ""optimizationId"": ""O-401d3d40b5a0e9f8c46c954a303f3ddd"",
                    ""projectId"": 23456789,
                    ""name"": ""Mia First Optimization Job"",
                    ""status"": ""running"",
                    ""nodeType"": ""O2-8"",
                    ""extremum"": ""max"",
                    ""criterion"": {
                        ""target"": ""TotalPerformance.PortfolioStatistics.SharpeRatio"",
                        ""extremum"": ""max"",
                        ""targetValue"": 1.0
                    },
                    ""created"": ""2024-01-03T00:00:00Z"",
                    ""psr"": 0.5,
                    ""sharpeRatio"": 1.5,
                    ""trades"": 12,
                    ""cloneId"": 24058693,
                    ""outOfSampleDays"": 30,
                    ""outOfSampleMaxEndDate"": ""2024-02-03T00:00:00Z"",
                    ""parameters"": [ { ""name"": ""rsi_period"", ""min"": 10, ""max"": 20, ""step"": 1 } ]
                }
            ],
            ""success"": true
        }";

        private const string ReadOptimizationResponse = @"{
            ""optimization"": {
                ""optimizationId"": ""O-401d3d40b5a0e9f8c46c954a303f3ddd"",
                ""snapshotId"": 24013333,
                ""projectId"": 23456789,
                ""name"": ""Mia First Optimization Job"",
                ""extremum"": ""min"",
                ""status"": ""completed"",
                ""nodeType"": ""O2-8"",
                ""parallelNodes"": 4,
                ""criterion"": {
                    ""target"": ""TotalPerformance.PortfolioStatistics.SharpeRatio"",
                    ""extremum"": ""max"",
                    ""targetValue"": 1.0
                },
                ""runtimeStatistics"": { ""Completed"": ""10"" },
                ""constraints"": [
                    {
                        ""target"": ""TotalPerformance.PortfolioStatistics.Drawdown"",
                        ""operator"": ""Less"",
                        ""targetValue"": 0.25
                    }
                ],
                ""parameters"": [ { ""name"": ""rsi_period"", ""min"": 10, ""max"": 20, ""step"": 1 } ],
                ""strategy"": ""QuantConnect.Optimizer.Strategies.GridSearchOptimizationStrategy"",
                ""requested"": ""2024-01-03T00:00:00Z"",
                ""optimizationTarget"": ""TotalPerformance.PortfolioStatistics.SharpeRatio"",
                ""targetValue"": 2.5,
                ""outOfSampleMaxEndDate"": ""2024-02-03T00:00:00Z"",
                ""outOfSampleDays"": 30,
                ""created"": ""2024-01-03T00:00:00Z"",
                ""psr"": 0.5,
                ""sharpeRatio"": 1.5,
                ""trades"": 12,
                ""cloneId"": 24058693
            },
            ""success"": true
        }";

        private const string EstimateOptimizationResponse = @"{
            ""estimate"": { ""estimateId"": ""6d5e2a2a32bf4b8b9d6f4a4a3a3f1b2c"", ""time"": 60, ""balance"": 10 },
            ""success"": true
        }";

        private const string SuccessResponse = @"{ ""success"": true, ""errors"": [] }";

        private static HashSet<OptimizationParameter> Parameters => new()
        {
            new OptimizationStepParameter("rsi_period", 10, 20, 1)
        };

        private static List<Constraint> Constraints => new()
        {
            new Constraint("TotalPerformance.PortfolioStatistics.Drawdown", ComparisonOperatorTypes.Less, 0.25m)
        };

        [Test]
        public void CreateOptimizationSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(ListOptimizationResponse);
            using var api = server.CreateApi();

            api.CreateOptimization(23456789, "Mia First Optimization Job", "TotalPerformance.PortfolioStatistics.SharpeRatio",
                "max", 1m, "QuantConnect.Optimizer.Strategies.GridSearchOptimizationStrategy",
                "5d1f2cba3a0ec7407c566614300502b5-173e0419674daf4144ce7c9931155ca8", Parameters, Constraints, 10m, "O2-8", 4);

            var request = server.GetSingleRequest();
            Assert.AreEqual("/optimizations/create", request.Path);
            Assert.AreEqual(23456789, request.Body["projectId"].Value<int>());
            Assert.AreEqual("Mia First Optimization Job", request.Body["name"].Value<string>());
            Assert.AreEqual("TotalPerformance.PortfolioStatistics.SharpeRatio", request.Body["target"].Value<string>());
            Assert.AreEqual("max", request.Body["targetTo"].Value<string>());
            Assert.AreEqual(1m, request.Body["targetValue"].Value<decimal>());
            Assert.AreEqual("QuantConnect.Optimizer.Strategies.GridSearchOptimizationStrategy", request.Body["strategy"].Value<string>());
            Assert.AreEqual("5d1f2cba3a0ec7407c566614300502b5-173e0419674daf4144ce7c9931155ca8", request.Body["compileId"].Value<string>());
            Assert.AreEqual(10m, request.Body["estimatedCost"].Value<decimal>());
            Assert.AreEqual("O2-8", request.Body["nodeType"].Value<string>());
            Assert.AreEqual(4, request.Body["parallelNodes"].Value<int>());
            Assert.AreEqual(1, ((JArray)request.Body["parameters"]).Count);
            Assert.AreEqual(1, ((JArray)request.Body["constraints"]).Count);
        }

        [Test]
        public void CreateOptimizationExposesTheDocumentedExtremum()
        {
            using var server = new StubApiServer(ListOptimizationResponse);
            using var api = server.CreateApi();

            var optimization = api.CreateOptimization(23456789, "Mia First Optimization Job",
                "TotalPerformance.PortfolioStatistics.SharpeRatio", "max", 1m,
                "QuantConnect.Optimizer.Strategies.GridSearchOptimizationStrategy",
                "5d1f2cba3a0ec7407c566614300502b5-173e0419674daf4144ce7c9931155ca8", Parameters, Constraints, 10m, "O2-8", 4);

            Assert.IsInstanceOf<Maximization>(optimization.Extremum);
            Assert.AreEqual(OptimizationStatus.Running, optimization.Status);
            Assert.AreEqual(1, optimization.Parameters.Count);
        }

        [Test]
        public void ListOptimizationsSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(ListOptimizationResponse);
            using var api = server.CreateApi();

            var optimizations = api.ListOptimizations(23456789);

            var request = server.GetSingleRequest();
            Assert.AreEqual("/optimizations/list", request.Path);
            Assert.AreEqual(23456789, request.Body["projectId"].Value<int>());
            Assert.AreEqual(1, optimizations.Count);
            Assert.IsInstanceOf<Maximization>(optimizations[0].Extremum);
            Assert.AreEqual(24058693, optimizations[0].CloneId);
        }

        [Test]
        public void ReadOptimizationExposesTheDocumentedResponse()
        {
            using var server = new StubApiServer(ReadOptimizationResponse);
            using var api = server.CreateApi();

            var optimization = api.ReadOptimization("O-401d3d40b5a0e9f8c46c954a303f3ddd");

            var request = server.GetSingleRequest();
            Assert.AreEqual("/optimizations/read", request.Path);
            Assert.AreEqual("O-401d3d40b5a0e9f8c46c954a303f3ddd", request.Body["optimizationId"].Value<string>());
            Assert.AreEqual(2.5m, optimization.TargetValue);
            Assert.IsInstanceOf<Minimization>(optimization.Extremum);
            Assert.AreEqual(24013333, optimization.SnapshotId);
            Assert.AreEqual(4, optimization.ParallelNodes);
            Assert.AreEqual(1, optimization.Constraints.Count);
            Assert.AreEqual("10", optimization.RuntimeStatistics["Completed"]);
        }

        [Test]
        public void EstimateOptimizationSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(EstimateOptimizationResponse);
            using var api = server.CreateApi();

            var estimate = api.EstimateOptimization(23456789, "Mia First Optimization Job",
                "TotalPerformance.PortfolioStatistics.SharpeRatio", "max", 1m,
                "QuantConnect.Optimizer.Strategies.GridSearchOptimizationStrategy",
                "5d1f2cba3a0ec7407c566614300502b5-173e0419674daf4144ce7c9931155ca8", Parameters, Constraints);

            var request = server.GetSingleRequest();
            Assert.AreEqual("/optimizations/estimate", request.Path);
            Assert.AreEqual(23456789, request.Body["projectId"].Value<int>());
            Assert.AreEqual("6d5e2a2a32bf4b8b9d6f4a4a3a3f1b2c", estimate.EstimateId);
            Assert.AreEqual(60, estimate.Time);
            Assert.AreEqual(10, estimate.Balance);
        }

        [Test]
        public void UpdateOptimizationSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(SuccessResponse);
            using var api = server.CreateApi();

            api.UpdateOptimization("O-401d3d40b5a0e9f8c46c954a303f3ddd", "New Optimization Name");

            var request = server.GetSingleRequest();
            Assert.AreEqual("/optimizations/update", request.Path);
            Assert.AreEqual("O-401d3d40b5a0e9f8c46c954a303f3ddd", request.Body["optimizationId"].Value<string>());
            Assert.AreEqual("New Optimization Name", request.Body["name"].Value<string>());
        }

        [TestCase(true, "/optimizations/abort")]
        [TestCase(false, "/optimizations/delete")]
        public void OptimizationIdOnlyEndpointsSendTheDocumentedRequest(bool abort, string path)
        {
            using var server = new StubApiServer(SuccessResponse);
            using var api = server.CreateApi();

            if (abort)
            {
                api.AbortOptimization("O-401d3d40b5a0e9f8c46c954a303f3ddd");
            }
            else
            {
                api.DeleteOptimization("O-401d3d40b5a0e9f8c46c954a303f3ddd");
            }

            var request = server.GetSingleRequest();
            Assert.AreEqual(path, request.Path);
            Assert.AreEqual("O-401d3d40b5a0e9f8c46c954a303f3ddd", request.Body["optimizationId"].Value<string>());
        }
    }
}
