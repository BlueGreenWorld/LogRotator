using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace LogRotator
{
    public static class XLinqExtensions
    {
        public static XElement GetMandatoryElement(this XElement element, XName name)
        {
            XElement retElement = element.Element(name);
            if (retElement != null)
                return retElement;
            throw new InvalidOperationException(string.Format("Mandatory element: {0} is missing from: {1}", name, element.Name));
        }

        public static XAttribute GetMandatoryAttribute(this XElement element, XName name)
        {
            XAttribute retAttribute = element.Attribute(name);
            if (retAttribute != null)
                return retAttribute;
            throw new InvalidOperationException(string.Format("Mandatory attribute: {0} is missing from element: {1}", name, element.Name));
        }
    }
}
