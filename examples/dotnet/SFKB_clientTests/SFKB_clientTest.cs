using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SFKB_API;

namespace SFKB_clientTests
{
    [TestClass]
    public class SfkbClientTest
    {
        private static Client Client;
        private Guid DatasetId;
        private static readonly Guid WrongDatasetId = new Guid("2fa85f64-5717-4562-b3fc-2c963f66afa6");
        private const string Ar5DatasetName = "ar5_test_23";
        private const string Ar5FlateFeatureLokalId = "20f893f2-5c8c-466f-b25b-d51ae98f1399";
        private const string Ar5GrenseFeatureLokalId = "0003f094-b524-4a5a-bb05-d69881df853a";

        [TestInitialize]
        public async Task InitAsync()
        {
            Client = General.GetClientWithBasicAuthentication();

            await GetDatasetsAsync();
        }        

        [TestMethod]
        public async Task TestGetDatasetsAsync()
        {
            var dataset = await GetDataset();

            Assert.IsTrue(dataset.Id == DatasetId, $"No dataset found with id {DatasetId}");
        }        

        [TestMethod]
        public void TestGetDatasetWithWrongId()
        {
            Exception exception = null;

            try
            {
                var result = Client.GetDatasetMetadataAsync(WrongDatasetId).Result;
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                var httpStatusIs403 = exception?.InnerException?.Message?.Contains("403") ?? false;

                Assert.IsTrue(httpStatusIs403, $"Wrong result when asking for non-existing Dataset");
            }
        }

        [TestMethod]
        public async Task TestReplacePolygonFeatureAsync()
        {
            await ReplaceByLokalIdAsync(DatasetId, Ar5FlateFeatureLokalId);
        }

        [TestMethod]
        public async Task TestReplaceReferencedLineFeatureAsync()
        {
            await ReplaceByLokalIdAsync(DatasetId, Ar5GrenseFeatureLokalId);
        }

        [TestMethod]
        public async Task TestGetLockedFeaturesByLokalIdAsync()
        {
            var locking = General.GetLocking();

            await LockAndSaveFeatureByLokalIdAsync(Ar5FlateFeatureLokalId, locking);

            await DeleteLocks(DatasetId, locking);
        }

        [TestMethod]
        public async Task TestGetFeaturesByLokalIdAsync()
        {
            await LockAndSaveFeatureByLokalIdAsync(Ar5GrenseFeatureLokalId, null);
        }

        private Task<Dataset> GetDataset()
        {
            return Client.GetDatasetMetadataAsync(DatasetId);
        }

        private async Task GetDatasetsAsync()
        {
            var datasets = await Client.GetDatasetsAsync();

            Assert.IsTrue(datasets.Count > 0, "No datasets returned");

            DatasetId = datasets.FirstOrDefault(d => d.Name == Ar5DatasetName).Id;
        }

        private async Task ReplaceByLokalIdAsync(Guid datasetId, string lokalId)
        {
            var locking = General.GetLocking();

            var tempFile = await LockAndSaveFeatureByLokalIdAsync(lokalId, locking);

            var datasetLocks = await Client.GetDatasetLocksAsync(datasetId, locking);

            Assert.IsTrue(datasetLocks?.Count() > 0, $"No dataset found for datasetId {datasetId}");

            var lockedLokalIds = datasetLocks.SelectMany(l => l?.Features?.Select(f => f.Lokalid))?.ToList();

            Assert.IsTrue(lockedLokalIds!= null && lockedLokalIds.Count > 0, $"No features locked for datasetId {datasetId} and lokalId {lokalId}");

            var wfsReplaceFile = Wfs.CreateReplaceTransaction(tempFile, lockedLokalIds);

            using (var featureStream = File.OpenRead(wfsReplaceFile))
            {
                var response = await Client.UpdateDatasetFeaturesAsync(datasetId, locking, featureStream);

                Assert.IsTrue(response.Features_replaced > 0, "No features updated");
            };

            await DeleteLocks(datasetId, locking);
        }

        private async Task DeleteLocks(Guid datasetId, Locking locking)
        {
            await Client.DeleteDatasetLocksAsync(datasetId, locking);

            var locks = await Client.GetDatasetLocksAsync(datasetId, locking);

            Assert.IsTrue(locks.Count() == 0, "Locks not deleted");
        }

        private async Task<string> LockAndSaveFeatureByLokalIdAsync(string lokalId, Locking locking)
        {
            var fileResponse = await Client.GetDatasetFeaturesAsync(DatasetId, locking, null, GetLokalIdQuery(lokalId));

            return General.WriteStreamToDisk(fileResponse);
        }

        private string GetLokalIdQuery(string lokalid)
        {
            return $"eq(*/identifikasjon/lokalid,{lokalid})";
        }

        //private BoundingBox GetExampleBbox()
        //{
        //    var ll1 = 365600;
        //    var ll2 = 7217500;
        //    var ur1 = 366100;
        //    var ur2 = 7217850;

        //    return new BoundingBox { Ll = new List<double> { ll1,  ll2 }, Ur = new List<double> { ur1, ur2 } };
        //}

        //private Stream GetExampleFeatureStream(string fileName)
        //{
        //    Assert.IsTrue(File.Exists(fileName), $"Example feature not found at {fileName}");

        //    return new StreamReader(fileName).BaseStream;
        //}

        //[TestMethod]
        //public void TestGetFeaturesWithBbox()
        //{
        //    var fileResponse = Client.GetDatasetFeaturesAsync(DatasetId, null, GetExampleBbox(), null).Result;

        //    WriteStreamToDisk(fileResponse);
        //}
    }
}
