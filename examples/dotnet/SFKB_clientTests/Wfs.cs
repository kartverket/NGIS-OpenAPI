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
        internal static string CreateReplaceTransaction(string tempFile, List<Guid> lockedLokalIds)
        {
            var newTempFile = Path.GetTempFileName();

            using (var featureStream = File.OpenRead(tempFile))
            {
                var featureXml = XElement.Load(featureStream);

                var replaceXml = GetReplaceXml(featureXml, lockedLokalIds);

                replaceXml.Save(newTempFile);
            }

            return newTempFile;
        }

        private static XElement GetReplaceXml(XElement featureXml, List<Guid> lockedLokalIds)
        {
            SetActiveNamespaceConstants(featureXml);

            var transactionElement = CreateTransactionElement();

            var count = 0;

            foreach (var featureMember in featureXml.Descendants(Names.xNameFeatureMember))
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
            var replaceElement = new XElement(Names.xNameReplace);

            replaceElement.Add(featureMember.FirstNode);

            replaceElement.Add(CreateFilter(currentLokalId));

            transactionElement.Add(replaceElement);
        }

        private static void GetActiveNamespacePrefix(XElement featureXml)
        {
            var prefixDeclaration = featureXml.DescendantsAndSelf().SelectMany(d => d.Attributes().Where(a => IsGeoNorgeSchema(a))).FirstOrDefault();

            Assert.IsTrue(prefixDeclaration != null, $"Unable to find a namespaceDeclaration starting with {Strings.GeoNorge}");

            Strings.activeSchemaPrefix = prefixDeclaration.Name.LocalName;

            Namespaces.activeNamespace = prefixDeclaration.Value;
        }

        private static bool IsGeoNorgeSchema(XAttribute a)
        {
            return a.Value.ToLower().StartsWith(Strings.GeoNorge);
        }

        private static void GetActiveSchemaLocation(XElement featureXml)
        {
            Strings.activeSchemaLocation = featureXml.Attribute(Names.xNameSchemaLocation).Value + " " + Strings.chlogfSchemaLocation;
        }

        private static XElement CreateChangeLogElement(XElement transactionElement, int count)
        {
            return new XElement(
                Names.xNameTransactionCollection,
                new XAttribute("startIndex", 1),
                new XAttribute("endIndex", count),
                new XAttribute("numberMatched", count),
                new XAttribute("numberReturned", count),
                new XAttribute("timeStamp", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffzzz")),
                Attributes.chlogfNamespaceDeclaration,
                Attributes.xsiNamespaceDeclaration,
                Attributes.appNamespaceDeclaration,
                Attributes.wfsNamespaceDeclaration,
                Attributes.gmlNamespaceDeclaration,
                Attributes.fesNamespaceDeclaration,
                new XAttribute(Names.xNameSchemaLocation, Strings.activeSchemaLocation),
                transactionElement
                );
        }

        private static XElement CreateFilter(Guid lokalId)
        {
            return new XElement(Names.xNameFilter,
                                new XElement(Names.xNamePropertyIsEqualTo,
                                    new XElement(Names.xNameValueReference, Strings.xpathExpressionLokalidFilter),
                                    new XElement(Names.xNameLiteral, lokalId)
                                    )
                                );
        }

        private static XElement CreateTransactionElement()
        {
            return new XElement(Namespaces.chlogfNamespace + "transactions",
                new XAttribute("version", "2.0.0"),
                new XAttribute("service", "WFS"));
        }

        private static Guid GetLokalId(XElement feature)
        {
            return new Guid(feature.Descendants(Namespaces.activeNamespace + "lokalId").Nodes().FirstOrDefault().ToString());
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