using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace CardArtist
{
    internal class XmlDynamicElement : DynamicObject
    {
        private readonly XElement Element;

        public XmlDynamicElement(XElement element)
        {
            Element = element;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            var attribute = Element.Attribute(binder.Name!);
            result = attribute != null ? new XmlDynamicAttribute(attribute) : null;

            return true;
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return Element.Attributes().Select(a => a.Name.ToString());
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            if (binder.Type == typeof(String))
            {
                result = ToString();
                return true;
            }
            else if (binder.Type == typeof(XElement))
            {
                result = Element;
                return true;
            }

            result = Convert.ChangeType(Element.Value, binder.Type);
            return true;
        }

        public override string ToString()
        {
            return Element.Nodes().Aggregate(new StringBuilder(), (sb, n) => sb.Append(n.ToString())).ToString();
        }

        public string Xml()
        {
            return Element.ToString();
        }

        public XmlDynamicElement[] Elements()
        {
            return Element.Elements().Select(e => new XmlDynamicElement(e)).ToArray();
        }

        public string Name()
        {
            return Element.Name.ToString();
        }

        public string LocalName()
        {
            return Element.Name.LocalName;
        }

        public XmlDynamicAttribute[] Attributes()
        {
            return Element.Attributes().Select(a => new XmlDynamicAttribute(a)).ToArray();
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
        {
            result = null;
            XElement? childElement;
            if (indexes.Length == 1 && indexes[0] is int index)
            {
                childElement = Element.Elements().ElementAtOrDefault(index);
            }
            else if (indexes.Length == 1 && indexes[0] is string name)
            {
                result = Element.Elements(name).Select(e => new XmlDynamicElement(e)).ToArray();
                return true;
            }
            else if (indexes.Length == 2 && indexes[0] is string name2 && indexes[1] is int index2)
            {
                childElement = Element.Elements(name2).ElementAtOrDefault(index2);
            }
            else
            {
                throw new ArgumentException("Invalid index type");
            }

            if (childElement != null)
            {
                result = new XmlDynamicElement(childElement);
            }
            return true;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
        {
            result = null;
            int argsNumber = args?.Length ?? 0;
            switch (binder.Name)
            {
                case "Name":
                    result = Name();
                    return true;
                case "LocalName":
                    result = LocalName();
                    return true;
                case "Xml" when argsNumber == 0:
                    result = Xml();
                    return true;
                case "Elements" when argsNumber == 0:
                    result = Elements();
                    return true;
                case "Attributes" when argsNumber == 0:
                    result = Attributes();
                    return true;
            }
            return false;
        }
    }

    internal class XmlDynamicAttribute : DynamicObject
    {
        private readonly XAttribute Attribute;

        public XmlDynamicAttribute(XAttribute attribute)
        {
            Attribute = attribute;
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            if (binder.Type == typeof(String))
            {
                result = Attribute.Value;
                return true;
            }
            else if (binder.Type == typeof(XAttribute))
            {
                result = Attribute;
                return true;
            }

            result = Convert.ChangeType(Attribute.Value, binder.Type);
            return true;
        }

        public string Name()
        {
            return Attribute.Name.ToString();
        }

        public string LocalName()
        {
            return Attribute.Name.LocalName;
        }

        public override string ToString()
        {
            return Attribute.Value;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
        {
            if (args == null || args.Length == 0)
            {
                switch (binder.Name)
                {
                    case "Name":
                        result = Name();
                        return true;
                    case "LocalName":
                        result = LocalName();
                        return true;
                }
            }
            result = null;
            return false;
        }
    }
}