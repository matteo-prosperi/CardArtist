using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace CardArtist
{
    abstract class TemplateException : Exception
    {
        public string Code { get; private set; }

        public TemplateException(string message, string code, Exception? exception = null) : base(message, exception)
        {
            Code = code;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(Message);
            sb.AppendLine();
            sb.AppendLine("Code:");
            sb.AppendLine();
            var lines = Regex.Split(Code, "\r\n|\r|\n");
            for (int i = 1; i < lines.Length; i++)
            {
                sb.AppendLine($"{i:D4} {lines[i]}");
            }
            PrintDiagnostics(sb);
            return sb.ToString();
        }

        protected abstract void PrintDiagnostics(StringBuilder sb);
    }

    class CompilationException : TemplateException
    {
        public ImmutableArray<Diagnostic> Diagnostics { get; private set; }
        public string Template { get; private set; }

        public CompilationException(string template, string code, ImmutableArray<Diagnostic> diagnostics) : base($"Compilation error for {template}.", code)
        {
            Diagnostics = diagnostics;
            Template = template;
        }

        protected override void PrintDiagnostics(StringBuilder sb)
        {
            sb.AppendLine();
            sb.AppendLine("Diagnostics:");
            foreach (var d in Diagnostics)
            {
                sb.AppendLine();
                sb.AppendLine(d.ToString());
            }
        }
    }

    class XamlGenerationException : TemplateException
    {
        public string Deck { get; private set; }
        public string CardId { get; private set; }

        public XamlGenerationException(string deck, string cardId, string code, Exception innerException) : base($"Xaml generation error for {deck}/{cardId}.", code, innerException)
        {
            Deck = deck;
            CardId = cardId;
        }

        protected override void PrintDiagnostics(StringBuilder sb)
        {
            sb.AppendLine();
            sb.AppendLine("Exception:");
            sb.AppendLine();
            sb.AppendLine(InnerException!.ToString());
        }
    }

    class XamlException : TemplateException
    {
        public string Deck { get; private set; }
        public string CardId { get; private set; }

        public XamlException(string deck, string cardId, string code, Exception innerException) : base($"Card image generation error for {deck}/{cardId}.", code, innerException)
        {
            Deck = deck;
            CardId = cardId;
        }

        protected override void PrintDiagnostics(StringBuilder sb)
        {
            sb.AppendLine();
            sb.AppendLine("Exception:");
            sb.AppendLine();
            sb.AppendLine(InnerException!.ToString());
        }
    }

    class XmlParsingException : TemplateException
    {
        public string DeckName { get; private set; }

        public XmlParsingException(string deckName, string code, Exception innerException) : base($"Xml parsing error for {deckName}.", code, innerException)
        {
            DeckName = deckName;
        }

        protected override void PrintDiagnostics(StringBuilder sb)
        {
            sb.AppendLine();
            if (InnerException is XmlException)
            {
                sb.AppendLine(InnerException.Message);
            }
            else
            {
                sb.AppendLine("Exception:");
                sb.AppendLine();
                sb.AppendLine(InnerException!.ToString());
            }
        }
    }

    class TemplateNotFoundException : Exception
    {
        public string DeckName { get; private set; }
        public string TemplateName { get; private set; }

        public TemplateNotFoundException(string deckName, string templateName) : base($"Cannot find template {templateName} referenced in {deckName}")
        {
            TemplateName = templateName;
            DeckName = deckName;
        }

        public override string ToString()
        {
            return Message;
        }
    }
}
