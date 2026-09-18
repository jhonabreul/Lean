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
    /// Tests for the object store endpoints, run against a loopback stub so no api credentials are needed
    /// </summary>
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class ObjectStoreApiTests
    {
        private const string OrganizationId = "5cad178b20a1d52567b534553413b691";

        private const string ListObjectStoreResponse = @"{
            ""path"": ""Mia"",
            ""objects"": [
                {
                    ""key"": ""Mia/Test"",
                    ""name"": ""Test"",
                    ""modified"": ""2021-11-26T15:18:27.693Z"",
                    ""mime"": ""application/json"",
                    ""folder"": true,
                    ""size"": 13
                }
            ],
            ""page"": 2,
            ""totalPages"": 7,
            ""objectStorageUsed"": 2437,
            ""objectStorageUsedHuman"": ""2.27 GB"",
            ""success"": true
        }";

        private const string PropertiesObjectStoreResponse = @"{
            ""metadata"": {
                ""key"": ""key1"",
                ""modified"": ""2021-11-26T15:18:27.693Z"",
                ""created"": ""2021-11-25T15:18:27.693Z"",
                ""size"": 24,
                ""md5"": ""1bc29b36f623ba82aaf6724fd3b16718"",
                ""mime"": ""application/json"",
                ""preview"": ""{}""
            },
            ""success"": true
        }";

        private const string SuccessResponse = @"{ ""success"": true, ""errors"": [] }";

        [Test]
        public void ListObjectStoreExposesTheDocumentedResponse()
        {
            using var server = new StubApiServer(ListObjectStoreResponse);
            using var api = server.CreateApi();

            var response = api.ListObjectStore(OrganizationId, "Mia");

            var request = server.GetSingleRequest();
            Assert.AreEqual("/object/list", request.Path);
            Assert.AreEqual(OrganizationId, request.Body["organizationId"].Value<string>());
            Assert.AreEqual("Mia", request.Body["path"].Value<string>());

            Assert.AreEqual("Mia", response.Path);
            Assert.AreEqual(2, response.Page);
            Assert.AreEqual(7, response.TotalPages);
            Assert.AreEqual(2437, response.ObjectStorageUsed);
            Assert.AreEqual("2.27 GB", response.ObjectStorageUsedHuman);
            Assert.AreEqual(1, response.Objects.Count);
            Assert.AreEqual("Mia/Test", response.Objects[0].Key);
            Assert.AreEqual("Test", response.Objects[0].Name);
            Assert.AreEqual(13, response.Objects[0].Size);
            Assert.IsTrue(response.Objects[0].IsFolder);
        }

        [Test]
        public void GetObjectStorePropertiesExposesTheDocumentedResponse()
        {
            using var server = new StubApiServer(PropertiesObjectStoreResponse);
            using var api = server.CreateApi();

            var response = api.GetObjectStoreProperties(OrganizationId, "key1");

            var request = server.GetSingleRequest();
            Assert.AreEqual("/object/properties", request.Path);
            Assert.AreEqual(OrganizationId, request.Body["organizationId"].Value<string>());
            Assert.AreEqual("key1", request.Body["key"].Value<string>());

            Assert.AreEqual("key1", response.Properties.Key);
            Assert.AreEqual(24, response.Properties.Size);
            Assert.AreEqual("1bc29b36f623ba82aaf6724fd3b16718", response.Properties.Md5);
            Assert.AreEqual("application/json", response.Properties.Mime);
            Assert.AreEqual("{}", response.Properties.Preview);
        }

        [Test]
        public void DeleteObjectStoreSendsTheDocumentedRequest()
        {
            using var server = new StubApiServer(SuccessResponse);
            using var api = server.CreateApi();

            api.DeleteObjectStore(OrganizationId, "key1");

            var request = server.GetSingleRequest();
            Assert.AreEqual("/object/delete", request.Path);
            Assert.AreEqual(OrganizationId, request.Body["organizationId"].Value<string>());
            Assert.AreEqual("key1", request.Body["key"].Value<string>());
        }
    }
}
