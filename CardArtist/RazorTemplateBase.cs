using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CardArtist
{
    public abstract class RazorTemplateBase
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Uri ProjectRoot { get; set; }
        public TextWriter Output { get; private set; }
        public dynamic Data { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public string Path(object relativePath) => new Uri(ProjectRoot, relativePath.ToString()).LocalPath;

        public void Explore(XElement? element, Func<dynamic, (bool Print, bool Explore)> elementAction, Action<string>? textAction = null)
        {
            if (element != null)
            {
                foreach (var node in element.Nodes())
                {
                    if (node is XElement child)
                    {
                        var dynamicElement = new XmlDynamicElement(child);
                        var res = elementAction(dynamicElement);
                        if (res.Print)
                        {
                            WriteLiteral($"<{child.Name}");
                            foreach (var attribute in child.Attributes())
                            {
                                WriteLiteral(attribute.ToString());
                            }
                            WriteLiteral(child.IsEmpty ? "/>" : ">");
                        }
                        if (res.Explore)
                        {
                            Explore(child, elementAction, textAction);
                        }
                        if (res.Print && !child.IsEmpty)
                        {
                            WriteLiteral($"</{child.Name}>");
                        }
                    }
                    else if (node is XText text)
                    {
                        (textAction ?? Write)(text.Value);
                    }
                }
            }
        }

        public void ClearAndInit(dynamic data)
        {
            Output = new StringWriter();
            Data = data;
        }

        protected virtual void Write(object value)
        {
            WriteLiteral(value?.ToString());
        }

        protected virtual void Write(string value)
        {
            Output.Write(SecurityElement.Escape(value));
        }

        protected void WriteLiteral(object value)
        {
            WriteLiteral(value?.ToString());
        }

        protected void WriteLiteral(string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                Output.Write(value);
            }
        }

        protected string? attributeEnd;

        protected void BeginWriteAttribute(string name, string begining, int startPosition, string ending, int endPosition, int thingy)
        {
            if (attributeEnd != null)
                throw new Exception("Wrong state for BeginWriteAttribute");
            WriteLiteral(begining);
            attributeEnd = ending;
        }

        protected void WriteAttributeValue(
            string prefix,
            int prefixOffset,
            object value,
            int valueOffset,
            int valueLength,
            bool isLiteral)
        {
            if (attributeEnd == null)
                throw new Exception("Wrong state for WriteAttributeValue");
            if (isLiteral)
                WriteLiteral(value);
            else
                Write(value);
        }
        protected virtual void EndWriteAttribute()
        {
            if (attributeEnd == null)
                throw new Exception("Wrong state for EndWriteAttribute");
            WriteLiteral(attributeEnd);
            attributeEnd = null;
        }

        public abstract Task ExecuteAsync();
    }
}
