using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace YandexPUSH
{
    public static class Extends
    {
        public static void AddAttribute(this XmlElement self, string attrName, string attrValue)
        {
            XmlAttribute attr = self.OwnerDocument.CreateAttribute(attrName);
            attr.Value = attrValue;
            self.Attributes.Append(attr);

        }

        public static XmlElement InsertElement(this XmlElement self, string elementName, string valueElement = "")
        {
            XmlElement element = self.OwnerDocument.CreateElement(elementName);
            if (valueElement.Length > 0)
                element.InnerText = valueElement;
            self.AppendChild(element);
            return element;
        }

        static public string Beautify(this XmlDocument doc)
        {
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace
            };
            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                doc.Save(writer);
            }
            return sb.ToString();
        }
    }
}
