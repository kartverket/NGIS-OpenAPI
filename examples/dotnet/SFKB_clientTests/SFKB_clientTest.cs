using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SFKB_API;

namespace SFKB_clientTests
{
    [TestClass]
    public class SfkbClientTest
    {
        private static Client Client;
        private static Guid DatasetId;
        private static readonly Guid WrongDatasetId = new Guid();
        private static readonly Guid WrongLokalId = new Guid();
        private const string Ar5DatasetName = "ar5_test_23";
        private const int epsg25833 = 25833;
        private Guid Ar5FlateFeatureLokalId = new Guid("20f893f2-5c8c-466f-b25b-d51ae98f1399");
        private Guid Ar5GrenseFeatureLokalId = new Guid("0003f094-b524-4a5a-bb05-d69881df853a");
        private const string clientString = "SystemTestClient";

        [TestInitialize]
        public async Task InitAsync()
        {
            Client = General.GetClientWithBasicAuthentication();

            await GetDatasetsAsync();

            await RemoveAllLocksIfExists();
        }

        [TestCleanup]
        public async Task CleanupAsync()
        {
            await RemoveAllLocksIfExists();
        }

        [TestMethod]
        public async Task TestGetDatasetsAsync()
        {
            var datasets = await GetDatasetsAsync();

            Assert.IsTrue(datasets.Any(dataset => dataset.Id == DatasetId), $"No dataset found with id {DatasetId}");
        }

        [TestMethod]
        public void TestGetDatasetWithWrongId()
        {
            Exception exception = null;

            try
            {
                var result = Client.GetDatasetMetadataAsync(clientString, WrongDatasetId, null, null).Result;
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
            await ReplaceByLokalIdAsync(DatasetId, Ar5FlateFeatureLokalId, "ArealressursFlate_ForReplace");
        }

        [TestMethod]
        public async Task TestReplaceReferencedLineFeatureAsync()
        {
            await ReplaceByLokalIdAsync(DatasetId, Ar5GrenseFeatureLokalId, "ArealressursGrense_ForReplace");
        }

        [TestMethod]
        public async Task TestGetLockedFeaturesByLokalIdAsync()
        {
            var locking = GetLocking();

            await LockAndSaveFeatureByLokalIdAsync(Ar5FlateFeatureLokalId, locking);
        }

        [TestMethod]
        public async Task TestGetFeaturesByLokalIdSansLockAsync()
        {
            await LockAndSaveFeatureByLokalIdAsync(Ar5GrenseFeatureLokalId, null);

            var locks = await Client.GetDatasetLocksAsync(clientString, DatasetId, GetLocking());

            Assert.IsTrue(locks.Count() == 0, $"Locks exists on dataset {DatasetId}");
        }

        [TestMethod]
        public async Task TestInsertAndDeleteNewFeaturesAsync()
        {
            var xml = General.GetExampleFile("ArealressursGrense");

            var lokalId = Wfs.GetLokalId(xml);
            
            var locking = GetLocking();

            var existingFeature = await LockAndSaveFeatureByLokalIdAsync(lokalId, locking);

            if (FileHasFeatures(existingFeature)) await DeleteByLokalIdAsync(existingFeature, lokalId, locking);

            var insertXml = Wfs.CreateInsertTransaction(xml, new List<Guid> { lokalId });

            Console.WriteLine($"Executing Insert");

            var response = await Execute(locking, insertXml);

            Assert.IsTrue(response.Features_created > 0, "No features updated");

            var newFeature = await LockAndSaveFeatureByLokalIdAsync(lokalId, locking);

            await DeleteByLokalIdAsync(newFeature, lokalId, locking);
        }

        private static async Task<Response> Execute(Locking_type locking, string xmlFile)
        {
            using (var featureStream = File.OpenRead(xmlFile))
            {
                var timer = new Stopwatch();
                
                timer.Start();

                var response = await Client.UpdateDatasetFeaturesAsync(clientString, DatasetId, locking ,null, null, featureStream);

                timer.Stop();

                Console.WriteLine($"Execute took {timer.Elapsed}");

                return response;
            };
        }

        [TestMethod]
        public async Task TestNonExistingLokalIdAsync()
        {
            var tempFile = await LockAndSaveFeatureByLokalIdAsync(WrongLokalId, GetLocking());

            Assert.IsFalse(FileHasFeatures(tempFile), $"Query with lokalId {WrongLokalId} gave unexpected result");
        }

        internal static Locking_type GetLocking()
        {
            return Locking_type.User_lock;
        }

        private async Task DeleteByLokalIdAsync(string tempFile, Guid lokalId, Locking_type locking)
        {
            await DeleteByLokalIdAsync(tempFile, new List <Guid> { lokalId }, locking);
        }

        private async Task DeleteByLokalIdAsync(string tempFile, List<Guid> lokalIds, Locking_type locking)
        {
            string deleteXmlPath = Wfs.CreateDeleteTransaction(tempFile, lokalIds);

            Console.WriteLine($"Executing Delete");

            var response = await Execute(locking, deleteXmlPath);

            Assert.IsTrue(response.Features_erased > 0, $"Feature with lokalIds ({string.Join(',', lokalIds)}) not deleted");
        }

        private bool FileHasFeatures(string tempFile)
        {
            var jObject =  JObject.Parse(File.ReadAllText(tempFile));

            return jObject.HasValues && jObject["features"].Count() > 0;

            //var xml = XElement.Load(tempFile);

            //return xml.HasElements && xml.Descendants().Count() > 0;
        }

        //private Task<Dataset> GetDataset()
        //{
        //    return Client.GetDatasetMetadataAsync(clientString, DatasetId);
        //}

        private async Task<ICollection<Anonymous>> GetDatasetsAsync()
        {
            var datasets = await Client.GetDatasetsAsync(clientString);

            Assert.IsTrue(datasets.Count > 0, "No datasets returned");

            DatasetId = datasets.FirstOrDefault(d => d.Name == Ar5DatasetName).Id;

            return datasets;
        }

        private async Task ReplaceByLokalIdAsync(Guid datasetId, Guid lokalId, string fileName)
        {
            var locking = GetLocking();

            var tempFile = await LockAndSaveFeatureByLokalIdAsync(lokalId, locking);

            var datasetLocks = await Client.GetDatasetLocksAsync(clientString, datasetId, locking);

            Assert.IsTrue(datasetLocks?.Count() > 0, $"No dataset found for datasetId {datasetId}");

            var lockedLokalIds = datasetLocks.SelectMany(l => l?.Features?.Select(f => f.Lokalid))?.ToList();

            Assert.IsTrue(lockedLokalIds != null && lockedLokalIds.Count > 0, $"No features locked for datasetId {datasetId} and lokalId {lokalId}");

            var xml = General.GetExampleFile(fileName);

            var wfsReplaceFile = Wfs.CreateReplaceTransaction(xml, lockedLokalIds);

            Console.WriteLine($"Executing Replace");

            var response = await Execute(locking, wfsReplaceFile);

            Assert.IsTrue(response.Features_replaced > 0, "No features updated");
        }

        private async Task RemoveAllLocksIfExists()
        {
            var locking = GetLocking();

            var locks = await Client.GetDatasetLocksAsync(clientString, DatasetId, locking);

            if (locks.Count == 0) return;

            await Client.DeleteDatasetLocksAsync(clientString, DatasetId, locking);

            locks = await Client.GetDatasetLocksAsync(clientString, DatasetId, locking);

            Assert.IsTrue(locks.Count == 0, "Locks not deleted");
        }

        private async Task<string> LockAndSaveFeatureByLokalIdAsync(Guid lokalId, Locking_type? locking)
        {
            var fileResponse = await Client.GetDatasetFeaturesAsync(
                clientString, 
                DatasetId, 
                locking,
                null,
                null,
                null, 
                References.Direct, 
                100,
                null,
                GetLokalIdQuery(lokalId));

            return General.WriteStreamToDisk(fileResponse);
        }

        private string GetLokalIdQuery(Guid lokalid)
        {
            return $"eq(*/identifikasjon/lokalid,{lokalid})";
        }

        //private List<double> GetExampleBbox()
        //{
        //    var ll1 = 365600;
        //    var ll2 = 7217500;
        //    var ur1 = 366100;
        //    var ur2 = 7217850;

        //    return new List<double>
        //    {
        //        ll1, 
        //        ll2,
        //        ur1, 
        //        ur2
        //    };
        //    //return new BoundingBox { Ll = new List<double> { ll1, ll2 }, Ur = new List<double> { ur1, ur2 } };
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
