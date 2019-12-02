using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using XmlConstants;

namespace SFKB_clientTests
{
    internal class Wfs
    {
        internal static string CreateReplaceWrappingForFeatures(string tempFile, List<Guid> lockedLokalIds)
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
            Strings.activeSchemaLocation = featureXml.Attribute(Names.xNameSchemaLocation).Value + " " + Strings.chlogfSchemaLocation;

            Namespaces.activeNamespace = featureXml.Attribute(Names.xNameAppPrefix).Value;

            var transactionElement = GetTransactionElement();

            var featureMembers = featureXml.Descendants(Names.xNameFeatureMember);

            foreach (var featureMember in featureMembers)
            {
                var currentLokalId = GetLokalId(featureMember);

                if (!lockedLokalIds.Contains(currentLokalId)) continue;
                
                var replaceElement = new XElement(Names.xNameReplace);

                replaceElement.Add(featureMember.FirstNode);

                replaceElement.Add(GetFilter(currentLokalId));

                transactionElement.Add(replaceElement);
            }

            var changeLogElement = GetChangeLogElement();

            changeLogElement.Add(transactionElement);

            var count = featureMembers.Count();

            changeLogElement.Add(
                new XAttribute("startIndex", 1),
                new XAttribute("endIndex", count),
                new XAttribute("numberMatched", count),
                new XAttribute("numberReturned", count),
                new XAttribute("timeStamp", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffzzz")));

            return changeLogElement;
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

        private static XElement GetChangeLogElement()
        {
            return new XElement(
                Names.xNameTransactionCollection,
                Attributes.chlogfNamespaceDeclaration,
                Attributes.xsiNamespaceDeclaration,
                Attributes.appNamespaceDeclaration,
                Attributes.wfsNamespaceDeclaration,
                Attributes.gmlNamespaceDeclaration,
                Attributes.fesNamespaceDeclaration,                
                new XAttribute(Names.xNameSchemaLocation, Strings.activeSchemaLocation)
                );
        }

        private static XElement GetFilter(Guid lokalId)
        {
            return new XElement(Names.xNameFilter,
                                new XElement(Names.xNamePropertyIsEqualTo,
                                    new XElement(Names.xNameValueReference, Strings.xpathExpressionLokalidFilter),
                                    new XElement(Names.xNameLiteral, lokalId)
                                    )
                                );
        }

        private static XElement GetTransactionElement()
        {
            return new XElement(Namespaces.chlogfNamespace + "transactions",
                new XAttribute("version", "2.0.0"),
                new XAttribute("service", "WFS"));
        }

        private static Guid GetLokalId(XElement feature)
        {
            return new Guid(feature.Descendants(Namespaces.activeNamespace + "lokalId").Nodes().FirstOrDefault().ToString());
        }
    }
}