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
using QuantConnect.Api;

namespace QuantConnect.Tests.API
{
    /// <summary>
    /// Tests for the AI assistance tool endpoints, run against a loopback stub so no api credentials are needed
    /// </summary>
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class AIToolsApiTests
    {
        private const string BacktestInitResponse = @"{
            ""state"": ""Error"",
            ""version"": 2.0,
            ""payload"": ""Runtime Error: name 'foo' is not defined"",
            ""payloadType"": ""String""
        }";

        private const string CodeCompletionResponse = @"{
            ""state"": ""End"",
            ""version"": 2.0,
            ""payload"": [ ""self.add_equity"", ""self.add_equity_option"" ],
            ""payloadType"": ""StringArray""
        }";

        private const string ErrorEnhanceResponse = @"{
            ""state"": ""End"",
            ""version"": 2.0,
            ""payload"": ""The symbol was not added to the algorithm"",
            ""payloadType"": ""String""
        }";

        private const string PEP8ConvertResponse = @"{
            ""state"": ""End"",
            ""version"": 2.0,
            ""payload"": { ""utils.py"": ""def add(a, b):\n    return a + b\n"" },
            ""payloadType"": ""StringDict""
        }";

        private const string SyntaxCheckResponse = @"{
            ""state"": ""End"",
            ""version"": 2.0,
            ""payload"": [ ""main.py(8): invalid syntax"" ],
            ""payloadType"": ""StringArray""
        }";

        private const string SearchResponse = @"{
            ""state"": ""End"",
            ""version"": 2.0,
            ""retrivals"": [
                {
                    ""url"": ""https://www.quantconnect.com/docs/v2/writing-algorithms/universes/index-options"",
                    ""score"": 0.320344448,
                    ""content"": ""Index options universes"",
                    ""type"": 2
                }
            ],
            ""messageId"": 7
        }";

        private static List<AIFile> Files => new()
        {
            new AIFile { Name = "main.py", Content = "fileContent" }
        };

        [Test]
        public void BacktestInitAIToolSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(BacktestInitResponse);
            using var api = server.CreateApi();

            var response = api.BacktestInitAITool(Language.Python, Files);

            var request = server.GetSingleRequest();
            Assert.AreEqual("/ai/tools/backtest-init", request.Path);
            Assert.AreEqual("Py", request.Body["language"].Value<string>());
            Assert.AreEqual("main.py", request.Body["files"][0]["name"].Value<string>());
            Assert.AreEqual("fileContent", request.Body["files"][0]["content"].Value<string>());

            Assert.AreEqual("Error", response.State);
            Assert.AreEqual(2.0m, response.Version);
            Assert.AreEqual("String", response.PayloadType);
            Assert.AreEqual("Runtime Error: name 'foo' is not defined", response.Payload);
        }

        [Test]
        public void CompleteCodeAIToolSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(CodeCompletionResponse);
            using var api = server.CreateApi();

            var response = api.CompleteCodeAITool(Language.Python, "self.add_eq", 10);

            var request = server.GetSingleRequest();
            Assert.AreEqual("/ai/tools/complete", request.Path);
            Assert.AreEqual("Py", request.Body["language"].Value<string>());
            Assert.AreEqual("self.add_eq", request.Body["sentence"].Value<string>());
            Assert.AreEqual(10, request.Body["responseSizeLimit"].Value<int>());

            Assert.AreEqual(2, response.Payload.Count);
            Assert.AreEqual("self.add_equity", response.Payload[0]);
        }

        [Test]
        public void CompleteCodeAIToolOmitsTheResponseSizeLimitWhenNotGiven()
        {
            using var server = new StubApiServer(CodeCompletionResponse);
            using var api = server.CreateApi();

            api.CompleteCodeAITool(Language.CSharp, "AddEq");

            var request = server.GetSingleRequest();
            Assert.AreEqual("C#", request.Body["language"].Value<string>());
            Assert.IsNull(request.Body["responseSizeLimit"]);
        }

        [Test]
        public void ErrorEnhanceAIToolSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(ErrorEnhanceResponse);
            using var api = server.CreateApi();

            var response = api.ErrorEnhanceAITool(Language.Python, "KeyError: SPY", "at main.py line 8");

            var request = server.GetSingleRequest();
            Assert.AreEqual("/ai/tools/error-enhance", request.Path);
            Assert.AreEqual("Py", request.Body["language"].Value<string>());
            Assert.AreEqual("KeyError: SPY", request.Body["error"]["message"].Value<string>());
            Assert.AreEqual("at main.py line 8", request.Body["error"]["stacktrace"].Value<string>());

            Assert.AreEqual("The symbol was not added to the algorithm", response.Payload);
        }

        [Test]
        public void PEP8ConvertAIToolSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(PEP8ConvertResponse);
            using var api = server.CreateApi();

            var response = api.PEP8ConvertAITool(Files);

            var request = server.GetSingleRequest();
            Assert.AreEqual("/ai/tools/pep8-convert", request.Path);
            Assert.IsNull(request.Body["language"]);
            Assert.AreEqual("main.py", request.Body["files"][0]["name"].Value<string>());

            Assert.AreEqual("def add(a, b):\n    return a + b\n", response.Payload["utils.py"]);
        }

        [Test]
        public void SyntaxCheckAIToolSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(SyntaxCheckResponse);
            using var api = server.CreateApi();

            var response = api.SyntaxCheckAITool(Language.Python, Files);

            var request = server.GetSingleRequest();
            Assert.AreEqual("/ai/tools/syntax-check", request.Path);
            Assert.AreEqual("Py", request.Body["language"].Value<string>());
            Assert.AreEqual(1, response.Payload.Count);
            Assert.AreEqual("main.py(8): invalid syntax", response.Payload[0]);
        }

        [Test]
        public void SearchAIToolSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(SearchResponse);
            using var api = server.CreateApi();

            var criteria = new List<SearchCriteria>
            {
                new SearchCriteria { Input = "option", Type = "Docs", Count = 1 }
            };
            var response = api.SearchAITool(Language.Python, criteria);

            var request = server.GetSingleRequest();
            Assert.AreEqual("/ai/tools/search", request.Path);
            Assert.AreEqual("Py", request.Body["language"].Value<string>());
            Assert.AreEqual("option", request.Body["criteria"][0]["input"].Value<string>());
            Assert.AreEqual("Docs", request.Body["criteria"][0]["type"].Value<string>());
            Assert.AreEqual(1, request.Body["criteria"][0]["count"].Value<int>());

            Assert.AreEqual(7, response.MessageId);
            Assert.AreEqual(1, response.Retrievals.Count);
            Assert.AreEqual(0.320344448m, response.Retrievals[0].Score);
            Assert.AreEqual(2, response.Retrievals[0].Type);
            Assert.AreEqual("Index options universes", response.Retrievals[0].Content);
        }
    }
}
