using System.Xml.Linq;

namespace XmlConstants
{
    internal class Strings
    {
        internal static string activeSchemaLocation;
        internal static readonly string xpathExpressionLokalidFilter = $"{appPrefix}:identifikasjon/{appPrefix}:Identifikasjon/{appPrefix}:lokalId";
        internal static readonly string chlogfSchemaLocation = "http://skjema.geonorge.no/standard/geosynkronisering/1.1/endringslogg http://skjema.geonorge.no/standard/geosynkronisering/1.1/endringslogg/changelogfile.xsd";
        internal static readonly string appPrefix = "app";
    }

    internal class Namespaces
    {
        internal static readonly XNamespace wfsNamespace = "http://www.opengis.net/wfs/2.0";
        internal static readonly XNamespace fesNamespace = "http://www.opengis.net/fes/2.0";
        internal static readonly XNamespace xsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";
        internal static readonly XNamespace gmlNamespace = "http://www.opengis.net/gml/3.2";
        internal static readonly XNamespace chlogfNamespace = "http://skjema.geonorge.no/standard/geosynkronisering/1.1/endringslogg";

        internal static XNamespace activeNamespace;

    }

    internal class Names
    {
        internal static readonly XName xNameSchemaLocation = Namespaces.xsiNamespace + "schemaLocation";
        internal static readonly XName xNameFeatureMember = Namespaces.gmlNamespace + "featureMember";
        internal static readonly XName xNameReplace = Namespaces.wfsNamespace + "Replace";
        internal static readonly XName xNameTransactionCollection = Namespaces.chlogfNamespace + "TransactionCollection";
        internal static readonly XName xNameAppPrefix = XNamespace.Xmlns + Strings.appPrefix;
        internal static readonly XName xNameFilter = Namespaces.fesNamespace + "Filter";
        internal static readonly XName xNamePropertyIsEqualTo = Namespaces.fesNamespace + "PropertyIsEqualTo";
        internal static readonly XName xNameValueReference = Namespaces.fesNamespace + "ValueReference";
        internal static readonly XName xNameLiteral = Namespaces.fesNamespace + "Literal";
    }

    internal class Attributes
    {
        internal static readonly XAttribute chlogfNamespaceDeclaration = new XAttribute(XNamespace.Xmlns + "chlogf", Namespaces.chlogfNamespace.NamespaceName);
        internal static readonly XAttribute xsiNamespaceDeclaration = new XAttribute(XNamespace.Xmlns + "xsi", Namespaces.xsiNamespace.NamespaceName);
        internal static readonly XAttribute wfsNamespaceDeclaration = new XAttribute(XNamespace.Xmlns + "wfs", Namespaces.wfsNamespace.NamespaceName);
        internal static readonly XAttribute appNamespaceDeclaration = new XAttribute(XNamespace.Xmlns + Strings.appPrefix, Namespaces.activeNamespace.NamespaceName);
        internal static readonly XAttribute gmlNamespaceDeclaration = new XAttribute(XNamespace.Xmlns + "gml", Namespaces.gmlNamespace.NamespaceName);
        internal static readonly XAttribute fesNamespaceDeclaration = new XAttribute(XNamespace.Xmlns + "fes", Namespaces.fesNamespace.NamespaceName);
    }

}