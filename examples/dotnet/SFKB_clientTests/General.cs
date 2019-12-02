using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SFKB_API;

namespace SFKB_clientTests
{
    internal class General
    {
        private const string ConfigLocation = @"../../../config.json";
        
        internal static Client GetClientWithBasicAuthentication()
        {
            var basicIdentification = File.Exists(ConfigLocation)
                ? GetIdentificationFromConfigFile()
                : GetIdentificationFromEnvironmentVariables();

            var byteArray = Encoding.ASCII.GetBytes(basicIdentification);

            var httpClient = new HttpClient();

            var basicValue = Convert.ToBase64String(byteArray);

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicValue);

            return new Client(httpClient);
        }

        private static string GetIdentificationFromEnvironmentVariables()
        {
            return $"{Environment.GetEnvironmentVariable("api_user")}:{Environment.GetEnvironmentVariable("api_pass")}";
        }

        private static string GetIdentificationFromConfigFile()
        {
            string basicIdentification;
            var config = JObject.Parse(File.ReadAllText(ConfigLocation));

            basicIdentification = $"{config["user"].ToString()}:{config["password"].ToString()}";
            return basicIdentification;
        }

        internal static string WriteStreamToDisk(FileResponse fileResponse)
        {
            var tempFile = Path.GetTempFileName();

            using (var fileStream = File.OpenWrite(tempFile))
            {
                fileResponse.Stream.CopyTo(fileStream);
            }

            Assert.IsTrue(File.Exists(tempFile), "No features saved to disk");

            var fileInfo = new FileInfo(tempFile);

            Assert.IsTrue(fileInfo.Length > 0, "Empty file saved to disk");

            return tempFile;
        }

        internal static Locking GetLocking()
        {
            return new Locking { Type = LockingType.User_lock };
        }
    }
}