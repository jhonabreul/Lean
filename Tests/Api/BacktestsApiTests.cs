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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using QuantConnect.Api;
using QuantConnect.Interfaces;

namespace QuantConnect.Tests.API
{
    /// <summary>
    /// Tests for the backtest endpoints, run against a loopback stub so no api credentials are needed
    /// </summary>
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class BacktestsApiTests
    {
        private const string BacktestId = "26c7bb06b8487cff1c7b3c44652b30f1";

        private const string SuccessfulBacktestResponse = @"{
            ""backtest"": {
                ""backtestId"": ""26c7bb06b8487cff1c7b3c44652b30f1"",
                ""name"": ""Determined Yellow Horse"",
                ""status"": ""Completed."",
                ""completed"": true,
                ""progress"": 1
            },
            ""debugging"": true,
            ""success"": true
        }";

        private const string SuccessfulBacktestListResponse = @"{
            ""backtests"": [],
            ""count"": 0,
            ""success"": true
        }";

        private const string SuccessfulInsightsResponse = @"{
            ""insights"": [],
            ""length"": 1337,
            ""success"": true
        }";

        private const string SuccessfulChartResponse = @"{
            ""chart"": { ""name"": ""Strategy Equity"", ""chartType"": 0, ""series"": {} },
            ""success"": true
        }";

        [Test]
        public void CreateBacktestSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(SuccessfulBacktestResponse);
            using var api = server.CreateApi();

            api.CreateBacktest(23456789, "5d1f2cba3a0ec7407c566614300502b5", "New Backtest");

            var request = server.GetSingleRequest();
            Assert.AreEqual("/backtests/create", request.Path);
            Assert.AreEqual(23456789, request.Body["projectId"].Value<int>());
            Assert.AreEqual("5d1f2cba3a0ec7407c566614300502b5", request.Body["compileId"].Value<string>());
            Assert.AreEqual("New Backtest", request.Body["backtestName"].Value<string>());
            Assert.IsNull(request.Body["parameters"]);
        }

        [Test]
        public void CreateBacktestSendsTheGivenParameters()
        {
            using var server = new StubApiServer(SuccessfulBacktestResponse);
            using var api = server.CreateApi();

            var parameters = new Dictionary<string, string> { { "ema-fast", "10" }, { "ema-slow", "20" } };
            api.CreateBacktest(23456789, "5d1f2cba3a0ec7407c566614300502b5", "New Backtest", parameters);

            var body = server.GetSingleRequest().Body;
            Assert.AreEqual("10", body["parameters"]["ema-fast"].Value<string>());
            Assert.AreEqual("20", body["parameters"]["ema-slow"].Value<string>());
        }

        [Test]
        public void CreateBacktestExposesTheDebuggingFlag()
        {
            using var server = new StubApiServer(SuccessfulBacktestResponse);
            using var api = server.CreateApi();

            var backtest = api.CreateBacktest(23456789, "5d1f2cba3a0ec7407c566614300502b5", "New Backtest");

            Assert.IsTrue(backtest.Success);
            Assert.IsTrue(backtest.Debugging);
            Assert.AreEqual(BacktestId, backtest.BacktestId);
        }

        [Test]
        public void ReadBacktestExposesTheDebuggingFlag()
        {
            using var server = new StubApiServer(SuccessfulBacktestResponse);
            using var api = server.CreateApi();

            var backtest = api.ReadBacktest(23456789, BacktestId, getCharts: false);

            var request = server.GetSingleRequest();
            Assert.AreEqual("/backtests/read", request.Path);
            Assert.IsTrue(backtest.Debugging);
            Assert.IsTrue(backtest.Completed);
        }

        [Test]
        public void ListBacktestsAsksForTheStatisticsByDefaultThroughTheInterface()
        {
            using var server = new StubApiServer(SuccessfulBacktestListResponse);
            using var api = server.CreateApi();

            var response = ((IApi)api).ListBacktests(23456789);

            var request = server.GetSingleRequest();
            Assert.AreEqual("/backtests/list", request.Path);
            Assert.IsTrue(request.Body["includeStatistics"].Value<bool>());
            Assert.AreEqual(0, response.Count);
        }

        [Test]
        public void ReadBacktestInsightsSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(SuccessfulInsightsResponse);
            using var api = server.CreateApi();

            var response = api.ReadBacktestInsights(23456789, BacktestId, 10, 60);

            var request = server.GetSingleRequest();
            Assert.AreEqual("/backtests/read/insights", request.Path);
            Assert.AreEqual(23456789, request.Body["projectId"].Value<int>());
            Assert.AreEqual(BacktestId, request.Body["backtestId"].Value<string>());
            Assert.AreEqual(10, request.Body["start"].Value<int>());
            Assert.AreEqual(60, request.Body["end"].Value<int>());
            Assert.AreEqual(1337, response.Length);
        }

        [Test]
        public void ReadBacktestChartSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(SuccessfulChartResponse);
            using var api = server.CreateApi();

            var response = api.ReadBacktestChart(23456789, "Strategy Equity", 1717801200, 1743462000, 100, BacktestId);

            var request = server.GetSingleRequest();
            Assert.AreEqual("/backtests/chart/read", request.Path);
            Assert.AreEqual(23456789, request.Body["projectId"].Value<int>());
            Assert.AreEqual(BacktestId, request.Body["backtestId"].Value<string>());
            Assert.AreEqual("Strategy Equity", request.Body["name"].Value<string>());
            Assert.AreEqual(100, request.Body["count"].Value<int>());
            Assert.AreEqual(1717801200, request.Body["start"].Value<int>());
            Assert.AreEqual(1743462000, request.Body["end"].Value<int>());
            Assert.AreEqual("Strategy Equity", response.Chart.Name);
        }

        [Test]
        public void ChartResponseExposesTheLoadingStatus()
        {
            var response = JsonConvert.DeserializeObject<ReadChartResponse>(
                @"{ ""status"": ""loading"", ""progress"": 0.42, ""success"": true }");

            Assert.AreEqual("loading", response.Status);
            Assert.AreEqual(0.42m, response.Progress);
            Assert.IsNull(response.Chart);
        }

        [Test]
        public void BacktestReportExposesTheGeneratingFlag()
        {
            var response = JsonConvert.DeserializeObject<BacktestReport>(
                @"{ ""generating"": true, ""success"": true }");

            Assert.IsTrue(response.Generating);
            Assert.IsNull(response.Report);
        }
    }
}
