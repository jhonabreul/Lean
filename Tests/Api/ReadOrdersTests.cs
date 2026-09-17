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
using QuantConnect.Orders;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace QuantConnect.Tests.API
{
    [TestFixture]
    public class ReadOrdersTests
    {
        private StubApiServer _server;
        private Api.Api _apiClient;

        [SetUp]
        public void SetUp()
        {
            _server = new StubApiServer();
            _apiClient = new StubbedApi(_server.BaseUrl);
            _apiClient.Initialize(123, "token", "");
        }

        [TearDown]
        public void TearDown()
        {
            _apiClient.DisposeSafely();
            _server.DisposeSafely();
        }

        [Test]
        public void ReadBacktestOrdersThrowsWhenWindowIsTooLarge()
        {
            Assert.Throws<ArgumentException>(() => _apiClient.ReadBacktestOrders(1, "id", 0, 101));
            Assert.IsNull(_server.LastRequestBody);
        }

        [Test]
        public void ReadLiveOrdersThrowsWhenWindowIsTooLarge()
        {
            Assert.Throws<ArgumentException>(() => _apiClient.ReadLiveOrders(1, 0, 101));
            Assert.IsNull(_server.LastRequestBody);
        }

        [Test]
        public void ReadBacktestOrdersAcceptsMaximumWindow()
        {
            Assert.That(() => _apiClient.ReadBacktestOrders(1, "id", 0, 100), Throws.Nothing);

            var payload = JObject.Parse(_server.LastRequestBody);
            Assert.AreEqual(0, payload["start"].Value<int>());
            Assert.AreEqual(100, payload["end"].Value<int>());
        }

        [Test]
        public void ReadLiveOrdersAcceptsMaximumWindow()
        {
            Assert.That(() => _apiClient.ReadLiveOrders(1, 0, 100), Throws.Nothing);

            var payload = JObject.Parse(_server.LastRequestBody);
            Assert.AreEqual(0, payload["start"].Value<int>());
            Assert.AreEqual(100, payload["end"].Value<int>());
        }

        [Test]
        public void ReadBacktestOrdersDefaultsEndToStartPlusOneHundred()
        {
            _apiClient.ReadBacktestOrders(1, "id", 500);

            var payload = JObject.Parse(_server.LastRequestBody);
            Assert.AreEqual(500, payload["start"].Value<int>());
            Assert.AreEqual(600, payload["end"].Value<int>());
        }

        [Test]
        public void ReadLiveOrdersDefaultsEndToStartPlusOneHundred()
        {
            _apiClient.ReadLiveOrders(1, 500);

            var payload = JObject.Parse(_server.LastRequestBody);
            Assert.AreEqual(500, payload["start"].Value<int>());
            Assert.AreEqual(600, payload["end"].Value<int>());
        }

        [Test]
        public void ReadBacktestOrdersReturnsTheTotalOrderCount()
        {
            var response = _apiClient.ReadBacktestOrders(1, "id");

            Assert.AreEqual(1234, response.Length);
            Assert.IsEmpty(response.Orders);
        }

        [Test]
        public void ReadAllBacktestOrdersPagesThroughTheWholeCollection()
        {
            _server.Responder = (path, body) => OrdersPage(body, 250, inclusiveEnd: false);

            var orders = _apiClient.ReadAllBacktestOrders(1, "id").ToList();

            Assert.AreEqual(250, orders.Count);
            Assert.AreEqual(250, orders.Select(x => x.Order.Id).Distinct().Count());
            Assert.AreEqual(0, orders[0].Order.Id);
            Assert.AreEqual(249, orders[249].Order.Id);
            CollectionAssert.AreEqual(new[] { 0, 100, 200 }, RequestedStarts());
        }

        [Test]
        public void ReadAllBacktestOrdersPagesThroughTheWholeCollectionWhenTheApiTakesEndAsInclusive()
        {
            _server.Responder = (path, body) => OrdersPage(body, 250, inclusiveEnd: true);

            var orders = _apiClient.ReadAllBacktestOrders(1, "id").ToList();

            Assert.AreEqual(250, orders.Count);
            Assert.AreEqual(250, orders.Select(x => x.Order.Id).Distinct().Count());
            Assert.AreEqual(0, orders[0].Order.Id);
            Assert.AreEqual(249, orders[249].Order.Id);
            CollectionAssert.AreEqual(new[] { 0, 101, 202 }, RequestedStarts());
        }

        [Test]
        public void ReadAllLiveOrdersPagesThroughTheWholeCollection()
        {
            _server.Responder = (path, body) => OrdersPage(body, 250, inclusiveEnd: false);

            var orders = _apiClient.ReadAllLiveOrders(1).ToList();

            Assert.AreEqual(250, orders.Count);
            Assert.AreEqual(250, orders.Select(x => x.Order.Id).Distinct().Count());
            CollectionAssert.AreEqual(new[] { 0, 100, 200 }, RequestedStarts());
        }

        [Test]
        public void ReadAllLiveInsightsPagesThroughTheWholeCollection()
        {
            _server.Responder = (path, body) => InsightsPage(body, 150);

            var insights = _apiClient.ReadAllLiveInsights(1).ToList();

            Assert.AreEqual(150, insights.Count);
            Assert.AreEqual(150, insights.Select(x => x.Id).Distinct().Count());
            Assert.AreEqual(Guid.Parse("00000000000000000000000000000001"), insights[0].Id);
            Assert.AreEqual("BTCUSD", insights[0].Symbol.Value);
            CollectionAssert.AreEqual(new[] { 0, 100 }, RequestedStarts());
        }

        [Test]
        public void ReadAllBacktestOrdersThrowsOnAnEmptyPage()
        {
            _server.Responder = (path, body) => EmptyOrdersResponse;

            Assert.Throws<InvalidOperationException>(() => _apiClient.ReadAllBacktestOrders(1, "id").ToList());
        }

        [Test]
        public void ReadAllBacktestInsightsThrowsWhenAPageFails()
        {
            _server.Responder = (path, body) => FailedInsightsResponse;

            var exception = Assert.Throws<WebException>(() => _apiClient.ReadAllBacktestInsights(1, "id").ToList());
            Assert.IsTrue(exception.Message.Contains("Insights are not available", StringComparison.InvariantCulture),
                exception.Message);
        }

        [Test]
        public void ReadAllBacktestOrdersOnlyRequestsThePagesThatAreEnumerated()
        {
            _server.Responder = (path, body) => OrdersPage(body, 250, inclusiveEnd: false);

            var orders = _apiClient.ReadAllBacktestOrders(1, "id").Take(1).ToList();

            Assert.AreEqual(1, orders.Count);
            Assert.AreEqual(1, _server.RequestBodies.Count);
        }

        private List<int> RequestedStarts()
        {
            return _server.RequestBodies.Select(x => JObject.Parse(x)["start"].Value<int>()).ToList();
        }

        private static string OrdersPage(string requestBody, int totalCount, bool inclusiveEnd)
        {
            var orders = new JArray();
            foreach (var index in RequestedIndexes(requestBody, totalCount, inclusiveEnd))
            {
                orders.Add(OrderJson(index));
            }

            return new JObject { ["orders"] = orders, ["length"] = totalCount, ["success"] = true }.ToString();
        }

        private static string InsightsPage(string requestBody, int totalCount)
        {
            var insights = new JArray();
            foreach (var index in RequestedIndexes(requestBody, totalCount, inclusiveEnd: false))
            {
                insights.Add(InsightJson(index + 1));
            }

            return new JObject { ["insights"] = insights, ["length"] = totalCount, ["success"] = true }.ToString();
        }

        private static IEnumerable<int> RequestedIndexes(string requestBody, int totalCount, bool inclusiveEnd)
        {
            var payload = JObject.Parse(requestBody);
            var start = payload["start"].Value<int>();
            var end = payload["end"].Value<int>();

            var last = Math.Min(inclusiveEnd ? end : end - 1, totalCount - 1);
            for (var index = start; index <= last; index++)
            {
                yield return index;
            }
        }

        private static JObject OrderJson(int id)
        {
            return new JObject
            {
                ["id"] = id,
                ["type"] = (int)OrderType.Market,
                ["status"] = (int)OrderStatus.Filled,
                ["time"] = "2024-01-01T00:00:00Z",
                ["quantity"] = 1,
                ["brokerId"] = new JArray(),
                ["symbol"] = new JObject { ["id"] = "SPY R735QTJ8XC9X", ["value"] = "SPY" }
            };
        }

        private static JObject InsightJson(int id)
        {
            return new JObject
            {
                ["id"] = id.ToStringInvariant("D32"),
                ["symbol"] = "BTCUSD XJ",
                ["ticker"] = "BTCUSD",
                ["type"] = "price",
                ["direction"] = "up",
                ["period"] = 5.0,
                ["created-time"] = 1520711961.0,
                ["close-time"] = 1520711961.0
            };
        }

        private class StubbedApi : Api.Api
        {
            private readonly string _baseUrl;

            public StubbedApi(string baseUrl)
            {
                _baseUrl = baseUrl;
            }

            protected override ApiConnection CreateApiConnection(int userId, string token)
            {
                return new ApiConnection(userId, token, _baseUrl);
            }
        }

        /// <summary>
        /// Local HTTP server that captures the request bodies and replies with a canned orders response
        /// unless a <see cref="Responder"/> is installed
        /// </summary>
        private class StubApiServer : IDisposable
        {
            private readonly HttpListener _listener;
            private readonly Thread _thread;
            private readonly List<string> _requestBodies = new();

            public string BaseUrl { get; }

            /// <summary>
            /// Takes the request path and body and returns the json to reply with
            /// </summary>
            public Func<string, string, string> Responder { get; set; } = (path, body) => EmptyOrdersResponse;

            public List<string> RequestBodies
            {
                get
                {
                    lock (_requestBodies)
                    {
                        return _requestBodies.ToList();
                    }
                }
            }

            public string LastRequestBody => RequestBodies.LastOrDefault();

            public StubApiServer()
            {
                BaseUrl = $"http://localhost:{GetAvailablePort()}/";
                _listener = new HttpListener();
                _listener.Prefixes.Add(BaseUrl);
                _listener.Start();

                _thread = new Thread(Listen) { IsBackground = true };
                _thread.Start();
            }

            public void Dispose()
            {
                _listener.Stop();
                _listener.Close();
                _thread.Join(TimeSpan.FromSeconds(5));
            }

            private void Listen()
            {
                while (_listener.IsListening)
                {
                    HttpListenerContext context;
                    try
                    {
                        context = _listener.GetContext();
                    }
                    catch (Exception)
                    {
                        // the listener was stopped
                        return;
                    }

                    string requestBody;
                    using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
                    {
                        requestBody = reader.ReadToEnd();
                    }
                    lock (_requestBodies)
                    {
                        _requestBodies.Add(requestBody);
                    }

                    var buffer = Encoding.UTF8.GetBytes(Responder(context.Request.Url.AbsolutePath, requestBody));
                    context.Response.ContentType = "application/json";
                    context.Response.ContentLength64 = buffer.Length;
                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                    context.Response.Close();
                }
            }

            private static int GetAvailablePort()
            {
                var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                var port = ((IPEndPoint)listener.LocalEndpoint).Port;
                listener.Stop();
                return port;
            }
        }

        private const string EmptyOrdersResponse = @"{ ""orders"": [], ""length"": 1234, ""success"": true }";

        private const string FailedInsightsResponse =
            @"{ ""insights"": [], ""length"": 0, ""success"": false, ""errors"": [""Insights are not available""] }";
    }
}
