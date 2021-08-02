using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using XmlConstants;

namespace SFKB_clientTests
{
    internal class Wfs
    {
        static string ar5SchemaLocation = "http://skjema.geonorge.no/SOSI/produktspesifikasjon/FKB-Ar5/4.6/FKB-AR546.xsd";
        static string ar5Namespace = "http://skjema.geonorge.no/SOSI/produktspesifikasjon/FKB-Ar5/4.6";

        internal static string CreateInsertTransaction(string tempFile, List<Guid> lokalIds)
        {
            var newTempFile = Path.GetTempFileName();

            var insertXml = XElement.Load(tempFile);

            SetActiveNamespaceConstants(insertXml);

            var insertElement = new XElement(Constants.xNameInsert);

            foreach (var lokalid in lokalIds) insertElement.Add(GetFeatureByLokalId(insertXml, lokalid));

            var transactionElement = CreateTransactionElement();

            transactionElement.Add(insertElement);

            var changeLogElement = CreateChangeLogElement(transactionElement, lokalIds.Count);

            changeLogElement.Save(newTempFile);

            return newTempFile;
        }

        private static XElement GetFeatureByLokalId(XElement xElement, Guid lokalid)
        {
            return xElement.DescendantsAndSelf().FirstOrDefault(d => d.Value == lokalid.ToString()).Parent.Parent.Parent;
        }

        private static JToken GetFeatureByLokalId(JObject jObject, Guid lokalid)
        {
            return jObject["features"].Where(f => f["properties"]["identifikasjon"]["lokalId"].ToString() == lokalid.ToString()).FirstOrDefault();
        }

        internal static string CreateReplaceTransaction(string tempFile, List<Guid> lockedLokalIds)
        {
            var newTempFile = Path.GetTempFileName();

            var featureJson = JObject.Parse(File.ReadAllText(tempFile));
            
            //var featureXml = XElement.Load(tempFile);

            var replaceXml = GetReplaceXml(featureJson, lockedLokalIds);

            replaceXml.Save(newTempFile);

            return newTempFile;
        }

        internal static string CreateDeleteTransaction(string tempFile, List<Guid> lokalIds)
        {
            var newTempFile = Path.GetTempFileName();

            var deleteJson = JObject.Parse(File.ReadAllText(tempFile));

            //var deleteXml = XElement.Load(tempFile);

            SetActiveNamespaceConstants(deleteJson);

            var transactionElement = CreateTransactionElement();

            foreach (var lokalid in lokalIds)
            {
                var feature = GetFeatureByLokalId(deleteJson, lokalid);
                
                transactionElement.Add(new XElement(
                    Constants.xNameDelete,
                    new XAttribute("typeName", $"{Constants.activeSchemaPrefix}:{feature["properties"]["featuretype"].ToString()}"),
                    CreateFilter(lokalid)
                    ));
            }

            var changeLogElement = CreateChangeLogElement(transactionElement, lokalIds.Count);

            changeLogElement.Save(newTempFile);
            
            return newTempFile;
        }

        private static XElement GetReplaceXml(XElement featureXml, List<Guid> lockedLokalIds)
        {
            SetActiveNamespaceConstants(featureXml);

            var transactionElement = CreateTransactionElement();

            var count = 0;

            foreach (var featureMember in featureXml.Descendants(Constants.xNameFeatureMember))
            {
                var currentLokalId = GetLokalId(featureMember);

                if (!lockedLokalIds.Contains(currentLokalId)) continue;

                AddReplaceFeatureMembertoTransaction(transactionElement, featureMember, currentLokalId);

                count++;
            }

            return CreateChangeLogElement(transactionElement, count);
        }

        private static XElement GetReplaceXml(JObject featureJson, List<Guid> lockedLokalIds)
        {
            SetActiveNamespaceConstants(featureJson);

            var transactionElement = CreateTransactionElement();

            var count = 0;

            foreach( var lokalid in lockedLokalIds)
            {
                var feature = GetFeatureByLokalId(featureJson, lokalid);

                 AddReplaceFeatureMembertoTransaction(transactionElement, feature, lokalid);

                count++;
            }

            return CreateChangeLogElement(transactionElement, count);
        }



        private static void SetActiveNamespaceConstants(XElement featureXml)
        {
            GetActiveNamespacePrefix(featureXml);

            GetActiveSchemaLocation(featureXml);
        }

        private static void SetActiveNamespaceConstants(JObject featureJson)
        {
            Constants.activeSchemaPrefix = "app";

            Constants.activeSchemaLocation = ar5Namespace + " " + ar5SchemaLocation + " " + Constants.chlogfSchemaLocation;
        }

        private static void AddReplaceFeatureMembertoTransaction(XElement transactionElement, XElement featureMember, Guid currentLokalId)
        {
            var replaceElement = new XElement(Constants.xNameReplace);

            replaceElement.Add(featureMember.FirstNode);

            replaceElement.Add(CreateFilter(currentLokalId));

            transactionElement.Add(replaceElement);
        }

        private static void AddReplaceFeatureMembertoTransaction(XElement transactionElement, JToken featureMember, Guid currentLokalId)
        {
            var replaceElement = new XElement(Constants.xNameReplace);

            replaceElement.Add(ConvertJsonToGml(featureMember));

            replaceElement.Add(CreateFilter(currentLokalId));

            transactionElement.Add(replaceElement);
        }

        private static object ConvertJsonToGml(JToken featureMember)
        {
            throw new NotImplementedException("Need to map from json to xml. Too lazy right now.");
        }

        private static void GetActiveNamespacePrefix(XElement featureXml)
        {
            var prefixDeclaration = featureXml.DescendantsAndSelf().SelectMany(d => d.Attributes().Where(a => IsGeoNorgeSchema(a))).FirstOrDefault();

            Assert.IsTrue(prefixDeclaration != null, $"Unable to find a namespaceDeclaration starting with {Constants.GeoNorge}");

            Constants.activeSchemaPrefix = prefixDeclaration.Name.LocalName;

            Constants.activeNamespace = prefixDeclaration.Value;
        }

        private static bool IsGeoNorgeSchema(XAttribute a)
        {
            return IsGeoNorgeSchema(a.Value);
        }

        private static bool IsGeoNorgeSchema(string a)
        {
            return a.ToLower().StartsWith(Constants.GeoNorge);
        }

        private static void GetActiveSchemaLocation(XElement featureXml)
        {
            var candidates = featureXml.DescendantsAndSelf().SelectMany(d => d.Attributes(Constants.xNameSchemaLocation));

            Assert.IsTrue(candidates != null && candidates.Count() > 0, "Unable to find schemaLocation declaration");

            Constants.activeSchemaLocation = string.Join(' ', candidates.Select(a => a.Value) ) + " " + Constants.chlogfSchemaLocation;
        }

        private static void GetActiveSchemaLocation(JObject featureJson)
        {
            var candidates = featureJson.DescendantsAndSelf().Where(d => d.ToString() ==Constants.xNameSchemaLocation);

            Assert.IsTrue(candidates != null && candidates.Count() > 0, "Unable to find schemaLocation declaration");

            Constants.activeSchemaLocation = string.Join(' ', candidates.FirstOrDefault() + " " + Constants.chlogfSchemaLocation);
        }

        private static XElement CreateChangeLogElement(XElement transactionElement, int count)
        {
            return new XElement(
                Constants.xNameTransactionCollection,
                new XAttribute("startIndex", 1),
                new XAttribute("endIndex", count),
                new XAttribute("numberMatched", count),
                new XAttribute("numberReturned", count),
                new XAttribute("timeStamp", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffzzz")),
                Constants.chlogfNamespaceDeclaration,
                Constants.xsiNamespaceDeclaration,
                new XAttribute(XNamespace.Xmlns + Constants.activeSchemaPrefix, ar5Namespace ),// Constants.activeNamespace.NamespaceName),
                Constants.wfsNamespaceDeclaration,
                Constants.gmlNamespaceDeclaration,
                Constants.fesNamespaceDeclaration,
                new XAttribute(Constants.xNameSchemaLocation, Constants.activeSchemaLocation),
                transactionElement
                );
        }

        internal static XElement CreateFilter(Guid lokalId)
        {
            return new XElement(Constants.xNameFilter,
                                new XElement(Constants.xNamePropertyIsEqualTo,
                                    new XElement(Constants.xNameValueReference, $"{Constants.activeSchemaPrefix}:identifikasjon/{Constants.activeSchemaPrefix}:Identifikasjon/{Constants.activeSchemaPrefix}:lokalId"),
                                    new XElement(Constants.xNameLiteral, lokalId)
                                    )
                                );
        }

        private static XElement CreateTransactionElement()
        {
            return new XElement(Constants.chlogfNamespace + "transactions",
                new XAttribute("version", "2.0.0"),
                new XAttribute("service", "WFS"));
        }

        private static Guid GetLokalId(XElement feature)
        {
            return new Guid(feature.Descendants(Constants.activeNamespace + "lokalId").Nodes().FirstOrDefault().ToString());
        }

        private static Guid GetLokalId(JToken featureMember)
        {
            return new Guid(featureMember["properties"]["identifikasjon"]["lokalId"].ToString());            
        }


        //private static bool FeatureReferencesActiveGeometry(XElement featureMember, IEnumerable<string> activeGeometryIds)
        //{
        //    var currentGmlIdElements = featureMember.Descendants().Where(d => d.Attribute(gmlNamespace + "id") != null && d.Attribute(gmlNamespace + "id").Value.StartsWith("QMS_"));

        //    var currentGmlIds = currentGmlIdElements.Select(i => i.Attribute(gmlNamespace + "id").Value.Split('_').Last());

        //    return currentGmlIds.Intersect(activeGeometryIds).Count() != 0;
        //}

        //private static IEnumerable<string> GetGeometryId(string ar5FeatureLokalId, IEnumerable<XElement> featureMembers)
        //{
        //    var activeFeature = featureMembers.FirstOrDefault(f => f.Descendants(activeNamespace + "lokalId").FirstOrDefault().Value == ar5FeatureLokalId);

        //    var gmlId = activeFeature.Descendants().Where(d => d.Attribute(gmlNamespace + "id") != null && d.Attribute(gmlNamespace + "id").Value.StartsWith("QMS_"));

        //    return gmlId.Select(i => i.Attribute(gmlNamespace + "id").Value.Split("_").Last());
        //}
    }
}