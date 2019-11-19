using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SFKB_API;

namespace SFKB_clientTests
{
    [TestClass]
    public class SfkbClientTest
    {
        private static Client Client;
        private static readonly Guid DatasetId = new Guid("0b88534d-c975-4b15-a8f3-da16a2101e29");
        private static readonly Guid WrongDatasetId = new Guid("2fa85f64-5717-4562-b3fc-2c963f66afa6");

        [TestInitialize]
        public void Init() {
            var username = Environment.GetEnvironmentVariable("api_user");
            var password = Environment.GetEnvironmentVariable("api_pass");

            var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");

            var httpClient = new HttpClient();

            var basicValue = Convert.ToBase64String(byteArray);
            
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicValue);

            Client = new Client(httpClient);
        }

        [TestMethod]
        public void TestGetDatasets()
        {
            var datasets = Client.GetDatasetsAsync().Result;
            Assert.IsTrue(datasets.Count > 0, "No datasets returned");
        }

        [TestMethod]
        public void TestGetDataset()
        {
            var dataset = Client.GetDatasetMetadataAsync(DatasetId).Result;
            Assert.IsTrue(dataset.Id == DatasetId, $"No dataset found with id {DatasetId}");
        }

        //[TestMethod]
        //public void TestGetDatasetWithWrongId()
        //{
        //    var dataset = Client.GetDatasetMetadataAsync(WrongDatasetId).Result;
        //    Assert.IsTrue(dataset.Id != DatasetId, $"Dataset found with wrong id {DatasetId}");
        //}

        //[TestMethod]
        //public void TestGetFeatures()
        //{
        //    var features = Client.GetDataAsync(DatasetId,Lock.FromJson(""),null,null).Result;
        //}
    }
}
