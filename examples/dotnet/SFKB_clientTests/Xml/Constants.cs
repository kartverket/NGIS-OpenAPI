using System.Xml.Linq;

namespace XmlConstants
{
    internal class Constants
    {
        internal static string activeSchemaLocation;
        internal static string activeSchemaPrefix;

        internal static readonly string chlogfSchemaLocation = "http://skjema.geonorge.no/standard/geosynkronisering/1.1/endringslogg http://skjema.geonorge.no/standard/geosynkronisering/1.1/endringslogg/changelogfile.xsd";
        internal static readonly string GeoNorge = "http://skjema.geonorge.no/";

        internal static readonly XNamespace wfsNamespace;
        internal static readonly XNamespace fesNamespace;
        internal static readonly XNamespace xsiNamespace;
        internal static readonly XNamespace gmlNamespace;
        internal static readonly XNamespace chlogfNamespace;

        internal static readonly XName xNameSchemaLocation;
        internal static readonly XName xNameFeatureMember;
        internal static readonly XName xNameReplace;
        internal static readonly XName xNameInsert;
        internal static readonly XName xNameDelete;
        internal static readonly XName xNameTransactionCollection;
        

        internal static readonly XName xNameFilter;
        internal static readonly XName xNamePropertyIsEqualTo;
        internal static readonly XName xNameValueReference;
        internal static readonly XName xNameLiteral;

        internal static readonly XAttribute chlogfNamespaceDeclaration;
        internal static readonly XAttribute xsiNamespaceDeclaration;
        internal static readonly XAttribute wfsNamespaceDeclaration;
        internal static readonly XAttribute gmlNamespaceDeclaration;
        internal static readonly XAttribute fesNamespaceDeclaration;

        internal static XNamespace activeNamespace;

        static Constants()
        {
            wfsNamespace = "http://www.opengis.net/wfs/2.0";
            fesNamespace = "http://www.opengis.net/fes/2.0";
            xsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";
            gmlNamespace = "http://www.opengis.net/gml/3.2";
            chlogfNamespace = "http://skjema.geonorge.no/standard/geosynkronisering/1.1/endringslogg";

            xNameSchemaLocation = xsiNamespace + "schemaLocation";
            xNameFeatureMember = gmlNamespace + "featureMember";
            xNameReplace = wfsNamespace + "Replace";
            xNameInsert = wfsNamespace + "Insert";
            xNameDelete = wfsNamespace + "Delete";
            xNameTransactionCollection = chlogfNamespace + "TransactionCollection";
            xNameFilter = fesNamespace + "Filter";
            xNamePropertyIsEqualTo = fesNamespace + "PropertyIsEqualTo";
            xNameValueReference = fesNamespace + "ValueReference";
            xNameLiteral = fesNamespace + "Literal";

            chlogfNamespaceDeclaration = new XAttribute(XNamespace.Xmlns + "chlogf", chlogfNamespace.NamespaceName);
            xsiNamespaceDeclaration = new XAttribute(XNamespace.Xmlns + "xsi", xsiNamespace.NamespaceName);
            wfsNamespaceDeclaration = new XAttribute(XNamespace.Xmlns + "wfs", wfsNamespace.NamespaceName);
            gmlNamespaceDeclaration = new XAttribute(XNamespace.Xmlns + "gml", gmlNamespace.NamespaceName);
            fesNamespaceDeclaration = new XAttribute(XNamespace.Xmlns + "fes", fesNamespace.NamespaceName);

        }
    }
}