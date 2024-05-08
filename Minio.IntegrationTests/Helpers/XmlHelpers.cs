using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

public static class XmlHelpers
{
    private static readonly XNamespace XsiNs = "https://www.w3.org/2001/XMLSchema-instance";

    private static readonly XName SchemaLocation = XsiNs + "schemaLocation";
    private static readonly XName NoNamespaceSchemaLocation = XsiNs + "noNamespaceSchemaLocation";

    public static string ToStringAlignAttributes(this XDocument document)
    {
        if (document == null) throw new ArgumentNullException(nameof(document));
        var stringBuilder = new StringBuilder();
        using var xmlWriter = XmlWriter.Create(stringBuilder, new XmlWriterSettings
        {
            Indent = true,
            OmitXmlDeclaration = true,
            NewLineOnAttributes = true
        });
        document.WriteTo(xmlWriter);
        return stringBuilder.ToString();
    }
    
    public static XDocument Normalize(XDocument source, XmlSchemaSet? schema = null)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        
        var havePsvi = false;
        if (schema != null)
        {
            source.Validate(schema, null, true);
            havePsvi = true;
        }

        return new XDocument(source.Declaration, source.Nodes().Select(n => n switch
        {
            // Remove comments, processing instructions, and text nodes that are
            // children of XDocument. Only white space text nodes are allowed as
            // children of a document, so we can remove all text nodes.
            XComment or XProcessingInstruction or XText => null,
            XElement e => NormalizeElement(e, havePsvi),
            _ => n
        }));
    }

    public static bool DeepEqualsWithNormalization(XElement elt1, XElement elt2)
    {
        return DeepEqualsWithNormalization(new XDocument(elt1), new XDocument(elt2));
    }

    public static bool DeepEqualsWithNormalization(XDocument doc1, XDocument doc2, XmlSchemaSet? schemaSet = null)
    {
        var d1 = Normalize(doc1, schemaSet);
        var d2 = Normalize(doc2, schemaSet);
        return XNode.DeepEquals(d1, d2);
    }

    private static IEnumerable<XAttribute> NormalizeAttributes(XElement element, bool havePsvi)
    {
        return element.Attributes()
            .Where(a => !a.IsNamespaceDeclaration &&
                        a.Name != SchemaLocation &&
                        a.Name != NoNamespaceSchemaLocation)
            .OrderBy(a => a.Name.NamespaceName)
            .ThenBy(a => a.Name.LocalName)
            .Select(
                a =>
                {
                    if (havePsvi)
                    {
                        var dt = a.GetSchemaInfo().SchemaType.TypeCode;
                        switch (dt)
                        {
                            case XmlTypeCode.Boolean:
                                return new XAttribute(a.Name, (bool)a);
                            case XmlTypeCode.DateTime:
                                return new XAttribute(a.Name, (DateTime)a);
                            case XmlTypeCode.Decimal:
                                return new XAttribute(a.Name, (decimal)a);
                            case XmlTypeCode.Double:
                                return new XAttribute(a.Name, (double)a);
                            case XmlTypeCode.Float:
                                return new XAttribute(a.Name, (float)a);
                            case XmlTypeCode.HexBinary:
                            case XmlTypeCode.Language:
                                return new XAttribute(a.Name, ((string)a).ToLowerInvariant());
                        }
                    }

                    return a;
                }
            );
    }

    private static XNode? NormalizeNode(XNode node, bool havePsvi)
    {
        return node switch
        {
            // Trim comments and processing instructions from normalized tree
            XComment or XProcessingInstruction => null,
            XElement e => NormalizeElement(e, havePsvi),
            // Only thing left is XCData and XText, so clone them
            _ => node
        };
    }

    private static XElement NormalizeElement(XElement element, bool havePsvi)
    {
        if (havePsvi)
        {
            var dt = element.GetSchemaInfo();
            return dt.SchemaType.TypeCode switch
            {
                XmlTypeCode.Boolean => new XElement(element.Name, NormalizeAttributes(element, havePsvi), (bool)element),
                XmlTypeCode.DateTime => new XElement(element.Name, NormalizeAttributes(element, havePsvi), (DateTime)element),
                XmlTypeCode.Decimal => new XElement(element.Name, NormalizeAttributes(element, havePsvi), (decimal)element),
                XmlTypeCode.Double => new XElement(element.Name, NormalizeAttributes(element, havePsvi), (double)element),
                XmlTypeCode.Float => new XElement(element.Name, NormalizeAttributes(element, havePsvi), (float)element),
                XmlTypeCode.HexBinary or XmlTypeCode.Language => new XElement(element.Name, NormalizeAttributes(element, havePsvi), ((string)element).ToLowerInvariant()),
                _ => new XElement(element.Name, NormalizeAttributes(element, havePsvi), element.Nodes().Select(n => NormalizeNode(n, havePsvi)))
            };
        }
        
        return new XElement(element.Name, NormalizeAttributes(element, havePsvi), element.Nodes().Select(n => NormalizeNode(n, havePsvi)));
    }
}