using System.Xml;

namespace lg2de.SimpleAccounting
{
    internal class ReportBase
    {
        protected ReportBase()
        {
        }

        protected void SetNodeAttribute(XmlNode node, string strName, string strValue)
        {
            XmlAttribute attr = node.OwnerDocument.CreateAttribute(strName);
            attr.Value = strValue;
            node.Attributes.SetNamedItem(attr);
        }
    }
}
