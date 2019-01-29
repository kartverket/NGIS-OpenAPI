using System;
using System.Net.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SFKB_API;

namespace SFKB_clientTests
{
    [TestClass]
    public class SfkbClientTest
    {
        private static readonly Client Client = new Client(new HttpClient());
        private static readonly Guid DatasetId = new Guid("07b59e3d-a4b6-4bae-ac4c-664d3dc3d778");
        private static readonly Guid WrongDatasetId = new Guid("2fa85f64-5717-4562-b3fc-2c963f66afa6");

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

        [TestMethod]
        public void TestGetDatasetWithWrongId()
        {
            var dataset = Client.GetDatasetMetadataAsync(WrongDatasetId).Result;
            Assert.IsTrue(dataset.Id != DatasetId, $"Dataset found with wrong id {DatasetId}");
        }

        //[TestMethod]
        //public void TestGetFeatures()
        //{
        //    var features = Client.GetDataAsync(DatasetId,Lock.FromJson(""),null,null).Result;
        //}
    }
}
