using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SFKB_clientTests
{
    internal class Wfs
    {
        private static readonly XNamespace gmlNamespace = "http://www.opengis.net/gml/3.2";
        private static readonly XNamespace wfsNamespace = "http://www.opengis.net/wfs/2.0";
        private static readonly XNamespace fesNamespace = "http://www.opengis.net/fes/2.0";
        private static readonly XNamespace xsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";
        private static readonly string appPrefix = "app";
        private static readonly XNamespace chlogfNamespace = "http://skjema.geonorge.no/standard/geosynkronisering/1.1/endringslogg";
        private static XNamespace activeNamespace;
        private static string activeSchemaLocation;
        private static readonly string chlogfSchemaLocation = "http://skjema.geonorge.no/standard/geosynkronisering/1.1/endringslogg http://skjema.geonorge.no/standard/geosynkronisering/1.1/endringslogg/changelogfile.xsd";

        internal static string CreateReplaceWrappingForFeatures(string tempFile, string ar5FeatureLokalId)
        {
            var newTempFile = Path.GetTempFileName();

            using (var featureStream = File.OpenRead(tempFile))
            {
                var featureXml = XElement.Load(featureStream);

                var replaceXml = GetReplaceXml(featureXml, ar5FeatureLokalId);

                replaceXml.Save(newTempFile);
            }

            return newTempFile;
        }

        private static XElement GetReplaceXml(XElement featureXml, string ar5FeatureLokalId)
        {          
            activeSchemaLocation = featureXml.Attribute(xsiNamespace + "schemaLocation").Value + " " + chlogfSchemaLocation;

            activeNamespace = featureXml.Attribute(XNamespace.Xmlns + appPrefix).Value;

            var changeLogElement = GetChangeLogElement();

            var transactionElement = GetTransactionElement();

            var featureMembers = featureXml.Descendants(gmlNamespace + "featureMember");

            var activeFeature = featureMembers.FirstOrDefault( f => f.Descendants(activeNamespace + "lokalId").FirstOrDefault().Value == ar5FeatureLokalId);

            var gmlId = activeFeature.Descendants().Where(d => d.Attribute(gmlNamespace + "id") != null && d.Attribute(gmlNamespace + "id").Value.StartsWith("QMS_")).FirstOrDefault();

            var activeGeometry = gmlId.Attribute(gmlNamespace + "id").Value.Split("_")[2];

            foreach (var featureMember in featureMembers)
            {
                var lokalId = GetLokalId(featureMember);

                if (lokalId != ar5FeatureLokalId)
                {
                    var currentGmlIds = featureMember.Descendants().Where(d => d.Attribute(gmlNamespace + "id") != null && d.Attribute(gmlNamespace + "id").Value.StartsWith("QMS_"));

                    if(!currentGmlIds.Any(id => id.Attribute(gmlNamespace + "id").Value.Contains(activeGeometry))) 
                        continue;
                }

                string xpathExpressionLokalidFilter = GetLokalIdXpath();

                var replaceElement = new XElement(wfsNamespace + "Replace");

                replaceElement.Add(featureMember.FirstNode);

                replaceElement.Add(GetFilter(lokalId, xpathExpressionLokalidFilter));

                transactionElement.Add(replaceElement);
            }
            
            changeLogElement.Add(transactionElement);

            var count = featureMembers.Count();

            changeLogElement.Add(
                new XAttribute("startIndex", 1),
                new XAttribute("endIndex", 1),
                new XAttribute("numberMatched", count),
                new XAttribute("numberReturned", count));
                       
            return changeLogElement;
        }

        private static XElement GetChangeLogElement()
        {
            return new XElement(chlogfNamespace + "TransactionCollection",
                new XAttribute(XNamespace.Xmlns + "chlogf", chlogfNamespace.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "xsi", xsiNamespace.NamespaceName),
                new XAttribute(XNamespace.Xmlns + appPrefix, activeNamespace.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "wfs", wfsNamespace.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "gml", gmlNamespace.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "fes", fesNamespace.NamespaceName),
                new XAttribute(xsiNamespace + "schemaLocation", activeSchemaLocation),                
                new XAttribute("timeStamp", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffzzz"))
                
                );
        }

        private static XElement GetFilter(string lokalId, string xpathExpressionLokalidFilter)
        {
            return new XElement(fesNamespace + "Filter",
                                new XElement(fesNamespace + "PropertyIsEqualTo",
                                    new XElement(fesNamespace + "ValueReference", xpathExpressionLokalidFilter),
                                    new XElement(fesNamespace + "Literal", lokalId)
                                    )
                                );
        }

        private static XElement GetTransactionElement()
        {
            return new XElement(chlogfNamespace + "transactions",
                new XAttribute("version", "2.0.0"),
                new XAttribute("service", "WFS"));
        }

        private static string GetLokalIdXpath()
        {
            return $"{appPrefix}:identifikasjon/{appPrefix}:Identifikasjon/{appPrefix}:lokalId";
        }

        private static string GetLokalId(XElement feature)
        {
            return feature.Descendants(activeNamespace + "lokalId").Nodes().FirstOrDefault().ToString();
        }
    }
}