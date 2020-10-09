using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;

namespace CardArtist
{
    class RendersGenerator : IDisposable
    {
        private Project Project;
        private bool DrawBorder;
        private bool Crop;
        private string RendersPath;
        private string DebugPath;
        private Dictionary<string, (RazorTemplateBase Template, string TemplateCode)>? TemplatesDictionary;
        private AssemblyLoadContext LoadContext;

        public RendersGenerator(Project project, bool drawBorder, bool crop)
        {
            Project = project;
            DrawBorder = drawBorder;
            Crop = crop;
            RendersPath = CreateEmptyFolder(project.FullPath, "Renders");
            DebugPath = CreateEmptyFolder(project.FullPath, "Debug");
            LoadContext = new AssemblyLoadContext("Generation context", true);
        }

        public async Task GenerateAsync()
        {
            try
            {
                var templates = Project.Templates;
                var cards = Project.Cards;
                if (cards != null && templates != null)
                {
                    TemplatesDictionary = templates.Items!
                        .Where(item => !(item is ProjectFolder))
                        .Select(item => CreateTemplate(item))
                        .ToDictionary(x => x.Name, x => (x.Template, x.TemplateCode));

                    foreach (var deck in cards.Items!.Where(item => !(item is ProjectFolder)))
                    {
                        await CreateDeckAsync(deck);
                    }
                }
            }
            finally
            {
                
            }
        }

        private string CreateEmptyFolder(string parent, string name)
        {
            var folderPath = Path.Join(parent, name);
            Directory.CreateDirectory(folderPath);
            foreach (var entry in Directory.EnumerateFiles(folderPath))
            {
                File.Delete(entry);
            }
            foreach (var entry in Directory.EnumerateDirectories(folderPath))
            {
                Directory.Delete(entry, true);
            }
            return folderPath;
        }

        class CardInfo
        {
            public string? Template;
            public double? Dpi;
            public int? Width;
            public int? Height;

            public void Parse(XElement e)
            {
                var template = e.Attribute("Template"!)?.Value;
                var dpi = e.Attribute("Dpi"!)?.Value;
                var width = e.Attribute("Width"!)?.Value;
                var height = e.Attribute("Height"!)?.Value;

                Template = template ?? Template;
                Dpi = dpi == null ? Dpi : double.Parse(dpi, CultureInfo.InvariantCulture);
                Width = width == null ? Width : int.Parse(width);
                Height = height == null ? Height : int.Parse(height);
            }
        }

        private async Task CreateDeckAsync(ProjectItem deck)
        {
            var deckName = Path.GetFileNameWithoutExtension(deck.FullPath);
            var deckRendersPath = Path.Join(RendersPath, deckName);
            var deckDebugPath = Path.Join(DebugPath, deckName);
            Directory.CreateDirectory(deckRendersPath);
            Directory.CreateDirectory(deckDebugPath);

            var deckText = File.ReadAllText(deck.FullPath);

            try
            {
                var root = XDocument.Parse(deckText, LoadOptions.PreserveWhitespace).Root!;
                foreach (var cardXmlElement in root.Elements())
                {
                    var cardInfo = new CardInfo();
                    cardInfo.Parse(root);
                    await GenerateCardAsync(deckName, cardXmlElement, cardInfo, deckRendersPath, deckDebugPath);
                }
            }
            catch (Exception e)
            {
                if (e is TemplateException)
                    throw;
                throw new XmlParsingException(deckName, deckText, e);
            }
        }

        private async Task GenerateCardAsync(string deckName, XElement cardXmlElement, CardInfo cardInfo, string deckRendersPath, string deckDebugPath)
        {
            cardInfo.Parse(cardXmlElement);
            if (cardInfo.Template == null || !TemplatesDictionary!.TryGetValue(cardInfo.Template, out var template))
            {
                throw new TemplateNotFoundException(deckName, cardInfo.Template ?? "");
            }

            dynamic cardData = new XmlDynamicElement(cardXmlElement);
            template.Template.ClearAndInit(cardData);
            string? cardName = cardData.Id;
            if (cardName == null)
            {
                throw new Exception("Missing card Id");
            }
            var cardRenderPath = Path.Join(deckRendersPath, cardName + ".png");
            var cardXamlPath = Path.Join(deckDebugPath, cardName + ".xaml");

            string xaml;
            try
            {
                await template.Template.ExecuteAsync();
                xaml = template.Template.Output.ToString()!;
                await File.WriteAllTextAsync(cardXamlPath, xaml);
            }
            catch (Exception e)
            {
                throw new XamlGenerationException(Path.GetFileName(deckRendersPath), cardName, template.TemplateCode, e);
            }

            try
            {
                using var reader = XmlReader.Create(new StringReader(xaml));
                var rootElement = (FrameworkElement)XamlReader.Load(reader);
                rootElement.DataContext = cardData; //Supports WPF data binding
                var cardBorder = (Border)rootElement.FindName("Card");
                if (!DrawBorder && cardBorder != null)
                {
                    cardBorder.BorderBrush = null;
                }

                rootElement.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                rootElement.Arrange(new Rect(0, 0, rootElement.DesiredSize.Width, rootElement.DesiredSize.Height));

                using var presentationSource = new HwndSource(new HwndSourceParameters()) { RootVisual = rootElement };

                var dpi = cardInfo.Dpi ?? 300;
                int width = cardInfo.Width ?? (int)(rootElement.DesiredSize.Width * dpi / 96);
                int height = cardInfo.Height ?? (int)(rootElement.DesiredSize.Height * dpi / 96);

                var bmp = new RenderTargetBitmap(
                    width,
                    height,
                    dpi, dpi, PixelFormats.Pbgra32);
                bmp.Render(rootElement);

                var img = BitmapFrame.Create(Crop && cardBorder != null ?
                    new CroppedBitmap(bmp, new Int32Rect(
                        (int)(cardBorder.Margin.Left * dpi / 96),
                        (int)(cardBorder.Margin.Top * dpi / 96),
                        (int)(cardBorder.ActualWidth * dpi / 96),
                        (int)(cardBorder.ActualHeight * dpi / 96))) :
                    bmp);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(img);
                using Stream s = File.Create(cardRenderPath);
                encoder.Save(s);
            }
            catch (Exception e)
            {
                throw new XamlException(Path.GetFileName(deckRendersPath), cardName, xaml, e);
            }
        }

        private (string Name, RazorTemplateBase Template, string TemplateCode) CreateTemplate(ProjectItem template)
        {
            var templateName = Path.GetFileNameWithoutExtension(template.FullPath);
            var templateDebugFile = Path.Join(DebugPath, templateName + ".cs");
            var templateText = File.ReadAllText(template.FullPath);

            var razorSourceDocument = RazorSourceDocument.Create(templateText, "template.razor");
#pragma warning disable CS0618
            var razorEngine = RazorEngine.Create(b =>
            {
                FunctionsDirective.Register(b);
                b.SetBaseType(typeof(RazorTemplateBase).FullName);
#pragma warning restore CS0618
            });
            var razorCodeDocument = RazorCodeDocument.Create(razorSourceDocument);
            razorEngine.Process(razorCodeDocument);
            var generatedCode = razorCodeDocument.GetCSharpDocument().GeneratedCode;

            var parsedCode = CSharpSyntaxTree.ParseText(SourceText.From(generatedCode));

            var formattedRoot = (CSharpSyntaxNode)parsedCode.GetRoot().NormalizeWhitespace();
            parsedCode = CSharpSyntaxTree.Create(formattedRoot);

            File.WriteAllText(templateDebugFile, parsedCode.ToString());

            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(asm => !asm.IsDynamic && !string.IsNullOrEmpty(asm.Location))
                .Select(asm => MetadataReference.CreateFromFile(asm.Location))
                .Concat(new MetadataReference[] {
                        MetadataReference.CreateFromFile(typeof(XElement).Assembly.Location),
                        MetadataReference.CreateFromFile(typeof(System.Dynamic.DynamicObject).Assembly.Location),
                        MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("Microsoft.CSharp")).Location),
                })
                .ToList();

            var projectRoot = new Uri(Project.FullPath + @"\");

            foreach (Match match in Regex.Matches(templateText, @"<!--\s*reference\s+([^>]+)\s*-->", RegexOptions.IgnoreCase))
            {
                var refName = match.Groups[1].Value;
                if (refName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    references.Add(MetadataReference.CreateFromFile(LoadContext.LoadFromAssemblyPath(new Uri(projectRoot, refName).LocalPath).Location));
                }
                else
                {
                    references.Add(MetadataReference.CreateFromFile(LoadContext.LoadFromAssemblyName(new AssemblyName(refName)).Location));
                }
            }

            using var templateAssemblyStream = new MemoryStream();
            using var templatePdbStream = new MemoryStream();
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Debug);
            var cSharpCompilation = CSharpCompilation.Create(Guid.NewGuid().ToString(), new List<SyntaxTree> { parsedCode }, references, options);
            var compilationResult = cSharpCompilation.Emit(templateAssemblyStream, templatePdbStream, options: new EmitOptions(debugInformationFormat: DebugInformationFormat.Pdb));

            if (!compilationResult.Success)
            {
                throw new CompilationException(templateName, templateText, compilationResult.Diagnostics);
            }

            templateAssemblyStream.Seek(0, SeekOrigin.Begin);
            templatePdbStream.Seek(0, SeekOrigin.Begin);
            var templateAssembly = LoadContext.LoadFromStream(templateAssemblyStream, templatePdbStream);
            var generatorType = templateAssembly.GetType("Razor.Template")!;
            var compiledTemplate = (RazorTemplateBase)generatorType.GetConstructor(Array.Empty<Type>())!.Invoke(Array.Empty<object>());

            compiledTemplate.ProjectRoot = projectRoot;

            return (templateName, compiledTemplate, templateText);
        }

        #region Disposable
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    LoadContext.Unload();
                }

                TemplatesDictionary = null;
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
