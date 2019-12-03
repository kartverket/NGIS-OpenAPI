using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XmlConstants;

namespace SFKB_clientTests
{
    internal class Wfs
    {
        internal static string CreateInsertTransaction(string tempFile, List<Guid> lokalIds)
        {
            var newTempFile = Path.GetTempFileName();

            var insertXml = XElement.Load(tempFile);

            SetActiveNamespaceConstants(insertXml);

            var insertElement = new XElement(Constants.xNameInsert);

            foreach (var lokalid in lokalIds)
            {
                var feature = insertXml.DescendantsAndSelf().FirstOrDefault(d => d.Value == lokalid.ToString()).Parent.Parent.Parent;

                insertElement.Add(feature);
            }

            var transactionElement = CreateTransactionElement();

            transactionElement.Add(insertElement);

            var changeLogElement = CreateChangeLogElement(transactionElement, lokalIds.Count);

            changeLogElement.Save(newTempFile);

            return newTempFile;
        }

        internal static string CreateReplaceTransaction(string tempFile, List<Guid> lockedLokalIds)
        {
            var newTempFile = Path.GetTempFileName();

            var featureXml = XElement.Load(tempFile);

            var replaceXml = GetReplaceXml(featureXml, lockedLokalIds);

            replaceXml.Save(newTempFile);

            return newTempFile;
        }

        internal static string CreateDeleteTransaction(string tempFile, List<Guid> lokalIds)
        {
            var newTempFile = Path.GetTempFileName();

            var deleteXml = XElement.Load(tempFile);

            SetActiveNamespaceConstants(deleteXml);

            var transactionElement = CreateTransactionElement();

            foreach (var lokalid in lokalIds)
            {
                var typeName = deleteXml.DescendantsAndSelf().FirstOrDefault(d => d.Value == lokalid.ToString()).Parent.Parent.Parent.Name;
                
                transactionElement.Add(new XElement(
                    Constants.xNameDelete,
                    new XAttribute("typeName", $"{Constants.activeSchemaPrefix}:{typeName.LocalName}"),
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

        private static void SetActiveNamespaceConstants(XElement featureXml)
        {
            GetActiveNamespacePrefix(featureXml);

            GetActiveSchemaLocation(featureXml);
        }

        private static void AddReplaceFeatureMembertoTransaction(XElement transactionElement, XElement featureMember, Guid currentLokalId)
        {
            var replaceElement = new XElement(Constants.xNameReplace);

            replaceElement.Add(featureMember.FirstNode);

            replaceElement.Add(CreateFilter(currentLokalId));

            transactionElement.Add(replaceElement);
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
            return a.Value.ToLower().StartsWith(Constants.GeoNorge);
        }

        private static void GetActiveSchemaLocation(XElement featureXml)
        {
            var candidates = featureXml.DescendantsAndSelf().SelectMany(d => d.Attributes(Constants.xNameSchemaLocation));

            Assert.IsTrue(candidates != null && candidates.Count() > 0, "Unable to find schemaLocation declaration");

            Constants.activeSchemaLocation = string.Join(' ', candidates.Select(a => a.Value) ) + " " + Constants.chlogfSchemaLocation;
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
                new XAttribute(XNamespace.Xmlns + Constants.activeSchemaPrefix, Constants.activeNamespace.NamespaceName),
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