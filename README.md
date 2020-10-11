# CardArtist

**CardArtist** is a tool aimed at automatically generating large numbers of playing card images based on a card database and graphic templates. **CardArtist** is designed to help hobbyists in the creation of new board games and card games from playtest, either physical or virtual, to professional printing by services like [The Game Crafter](https://www.thegamecrafter.com/).

**CardArtist** is currently in early beta stage and, while functional, it is still unstable.

The techinical design of **CardArtist** is discussed in the following blog posts. These provide insight about software architecture and programming techniques, they are not documentation about how to use **CardArtist**, for that see below.
- [Creating your own .NET DynamicObject. Why, when and how.](https://matteo.tech.blog/2020/09/21/creating-your-own-net-dynamicobject-why-when-and-how/)
- [Automating graphic design with WPF.](https://matteo.tech.blog/2020/10/05/automating-graphic-design-with-wpf/)

## System requirements

**CardArtist** runs exclusively on Windows and requires [.NET 5](https://dotnet.microsoft.com/download/dotnet/5.0).

## Project goals, non-goals and suggested third-party software

**CardArtist** is exclusively aimed at the batch generation of images.

**CardArtist** is NOT meant to provide:
- A text editor or an IDE
- A preview tool for XAML or WPF
- A C# debugger

There is very good free software that can be used in conjunction with **CardArtist**, these are a few suggestions:
- Card database (XML) and graphic templates ([Razor](https://docs.microsoft.com/en-us/aspnet/web-pages/overview/getting-started/introducing-razor-syntax-c)) editing: [Visual Studio Code](https://code.visualstudio.com/)
- XAML/WPF preview: [Blend for Visual Studio Community](https://visualstudio.microsoft.com/vs/community/)

## Creating a project
A **CardArtist** project consists of a folder having two subfolders: _Cards_ and _Templates_. **CardArtist** will automatically create this folder structure when using _Load Project_ and selecting a folder.

### Cards

The _Cards_ subfolder contains the card database which is composed of "decks": XML files like the following:
```
<Deck Template="t1" Dpi="300">
    <Card Id="1">
    </Card>
    <Card Id="2">
    </Card>
</Deck>
```
Each _deck_ file will result, when rendering the cards, in the generation of a separate folder. In the example above, the _deck_ references the _template_ file named `t1.razor`. The two cards from this deck will be rendered at `300` DPIs resulting in two files named `1.png` and `2.png`.

The user can add attributes and children to the `Card` elements, such data can be referenced from the template.

It is possible, but not required, to specify the exact pixel size of the images to be generated: `<Deck Template="t1" Dpi="300" Width="825" Height="1125">`.

Each `Card` element can optionally override some of the properties of the `Deck`: `<Card Id="1" Template="t2">`.

### Templates

The _Templates_ subfolder contains _Razor_ files used to generate _XAML_/_WPF_ layouts for each card.

[Razor](https://docs.microsoft.com/en-us/aspnet/web-pages/overview/getting-started/introducing-razor-syntax-c) is a templating language normally used to generate HTML files. **CardArtist** uses _Razor_ to generate XAML files.

[XAML](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/fundamentals/xaml) is a language used to describe user interfaces. **CardArtist** uses XAML to describe the [WPF](https://docs.microsoft.com/en-us/visualstudio/designers/getting-started-with-wpf) layout of each card.
See [here](https://matteo.tech.blog/2020/10/05/automating-graphic-design-with-wpf/) for a more detailed explanation of how this works and why WPF a great choice for drawing playing cards.

An empty _template_ file looks like the following:
```
@using System
<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      Background="White"
      Width="2.75in"
      Height="3.75in">
    <Border x:Name="Card"
            Width="2.5in"
            Height="3.5in"
            Margin="0.125in"
            Padding="0.125in"
            CornerRadius="10"
            BorderBrush="Black"
            BorderThickness="1">
        <Grid x:Name="SafeArea">
        </Grid>
    </Border>
</Grid>"
```

The _template_ above defines an area called `Card`, in this case 2.5 by 3.5 inches.

When having the cards printed professionally there may be a misalignment between printing and cut. In this case, the worst case misalignment is expected to be 0.125 inches. For this reason, the `Card` area has an external `Margin` of 0.125 inches and a corresponding internal `Padding`. The user should let the card background _bleed_ into the whole 2.75 by 3.75 inches area while all graphic elements should be inside the 2.25 by 3.25 inches `SafeArea`.

**CardArtist** allows to choose whether the rendered card images should contain the _bleed_ area (good for professional card printing) or be cropped so that only the `Card` area is visible (good for at-home printing or for virtual playtesting).

### Rendering output

When rendering cards, **CardArtist** will save the generated images into the _Renders_ project folder. It will also save the intermediate files (XAML and C#) used for the image generation into the _Debug_ folder.

## Template functionalities

### Referencing the card data in the template

Given the following _deck_ file
```
<Deck Template="t1" Dpi="300">
    <Card Id="1" Title="Ride the Jackrabbit" Image="./Jackrabbit.jpg">
        <Content>
            <Paragraph FontSize="9"><Bold>HERE IT IS!</Bold></Paragraph>
            <Paragraph FontSize="9">Along Arizona's stretch of Route 66, halfway between Holbrook and Winslow, you can find the <Bold>Jack Rabbit Trading Post</Bold>.</Paragraph>
            <Paragraph FontSize="9">Travelers of Route 66 can see billboards advertising this convenience store and gift shop as early as Missouri.</Paragraph>
        </Content>
    </Card>
</Deck>
```
the card data can be referenced from the template:
```
<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      Background="Black">
    <Border x:Name="Card"
            Width="2.5in" Height="3.5in"
            Margin="0.125in" Padding="0.125in"
            CornerRadius="10" BorderBrush="White" BorderThickness="1">
        <Grid x:Name="SafeArea">
            <Border CornerRadius="8" Background="White"
                    HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="*"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="*"/>
                    </Grid.RowDefinitions>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="0.2in"/>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="0.2in"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock Grid.Column="1" Grid.Row="0"
                               HorizontalAlignment="Center"
                               FontWeight="Bold" FontStyle="Italic">
                        @Data.Title
                    </TextBlock>
                    <Image Grid.Column="1" Grid.Row="1"
                        HorizontalAlignment="Stretch" VerticalAlignment="Stretch"
                        Stretch="UniformToFill" Source="@Path(Data.Image)" />
                    <RichTextBox Grid.Column="1" Grid.Row="3"
                                 BorderBrush="Transparent" Background="Transparent">
                        <FlowDocument>
                            @{ WriteLiteral(Data["Content", 0]); }
                        </FlowDocument>
                    </RichTextBox>
                </Grid>
            </Border>
        </Grid>
    </Border>
</Grid>
```

- `@Data.Title` copies the value of the `Title` attribute into the XAML layout.
- `@Path(Data.Image)` is a two-step operation: `Data.Image` accesses the `Image` attribute and `Path` converts it to an absolute file path so that it can be used by WPF.
- `@{ WriteLiteral(Data["Content", 0]); }` is a block of code doing multiple operations:
  - `@{` starts a section of _C#_ code
  - `Data["Content", 0]` accesses the first (number `0`) XML element named `Content` from the card data
  - `WriteLiteral` copies the inner XML from the `Content` element into the XAML layout without performing any XML escaping.

### Data access syntax

`@Data` provides access to the XML element representing the card.

An XML element can be interacted with in the following ways:
- Converting (casting) an XML element to `string` or calling the `.ToString()` method returns the inner text and descendant elements.
- Converting (casting) an XML element to a numerical type attempts a conversion of the inner text to number.
- An XML element can be converted (cast) to `System.Xml.Linq.XElement`.
- Calling the `.Xml()` method returns a string representing the element and all of its content.
- Calling the `.Elements()` method returns an array of the children elements.
- Calling the `.Attributes()` method returns an array of element's attributes.
- Calling the `.Name()` or `.LocalName()` methods return the qualified and unqualified name of the element.
- Accessing a property of an XML element returns the attribute of the same name or `null`.
- An XML element's children can be accessed with the array syntax:
  - `["Foo", 0]` returns the first (number `0`) child element named `Foo` or `null`.
  - `["Foo"]` returns an array containing all elements named `Foo`.
  - `[0]` returns the first (number `0`) child element or `null`.

An XML attribute can be interacted with in the following ways:
- Converting (casting) an XML attribute to `string` or calling the `.ToString()` method returns its value.
- Converting (casting) an XML attribute to a numerical type attempts a conversion of its value to number.
- An XML attribute can be converted (cast) to `System.Xml.Linq.XAttribute`.
- Calling the `.Name()` or `.LocalName()` methods return the qualified and unqualified name of the attribute.

These syntaxes can be chained as needed. For example:
- `@Data["Dice", 0]?.Die1` returns the `Die1` attribute of the first `Dice` child element (`<Card Id="1"><Dice Die1="Blue"></Card>`). The [`?` operator](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/member-access-operators#null-conditional-operators--and-) avoids an error in case a `Dice` element is not present.
- `@foreach(var paragraph in Data["Text", 0]["Paragraph"])` [iterates](https://docs.microsoft.com/en-us/aspnet/core/mvc/views/razor#control-structures) over all the elements named `Paragraph` inside the first `Text` element.

### Using files

The `Path` function can be used when referencing a file from the project folder in order to convert a local path to an absolute path which is understandable by WPF.

For example `<Card Id="1" Picture="./Foo.png" />` can be referenced like this in the template:
```
<Image Source="@Path(Data.Picture)" />
```

### Functions

_Raror_ allows to specify _C#_ code by adding a `@functions{ }` area in the template (see [here](https://docs.microsoft.com/en-us/aspnet/core/mvc/views/razor#directives)). This allows to create variables, functions and even define new types.

### Mapping XML to XAML

The `Explore` function allows to map complex XML data to XAML. The full definition of the `Explore` function is:
```
void Explore(XElement element,
             Func<dynamic, (bool Print, bool Explore)> elementAction,
             Action<string> textAction = null)
```
The function will explore the descendants of an element, in order, and invoke the provided callback functions for each element and text block encountered.
- The `element` parameter is the element to explore.
- The `elementAction` parameter is the callback function that will be invoked for each descendant element. If the callback returns `Print=true`, `Explore` will print the current element (opening and closing tags only, including attributes, not including content) as XAML. If the callback returns `Explore=true`, the `Explore` function will iterate over this element children.
- The `textAction` parameter is the callback function that will be invoked for each descendant block of text. If the `textAction` parameter is not provided, the `Explore` function will simply write each text block as XAML.

For example, the following XML data
```
<Card Id="1">
    <Text>
        <Paragraph><Use /></Paragraph>
        <Paragraph>Add <Bold>1</Bold> resource.</Paragraph>
    </Text>
</Card>
```
can be converted to XAML using `Explore`:
```
    @{
        Explore((XElement)Data["Text", 0], MapElementToXaml);
    }

    @functions{
        (bool Print, bool Explore) MapElementToXaml(dynamic e)
        {
            if (e.Name() == "Use")
            {
                //Convert the shorthand "<Use />" element to a XAML image
                <Image Source="@Path(@"Resources\Use.png")" />

                //Don't print the "<Use />" element, don't explore its descendants
                return (false, false);
            }

            //Any other element and their descendants should be printed without modifications
            return (true, true);
        }
    }
```

In some case the text portions of the XML data must be converted as well:
```
    @{
        Explore((XElement)Data["Text", 0], MapElementToXaml, MapTextToXaml);
    }

    @functions{
        void MapTextToXaml(string text)
        {
            <TextBlock xml:space="preserve">@text</TextBlock>
        }
    }
```

### Referencing external libraries

It is possible to reference _.dll_ libraries from the template using a `reference` XML comment. `reference` comments use paths relative to the project folder.

Once referenced, the library can be used:
- from _C#_ code, optionally adding a `@using` statement;
- from _XAML_, by adding a `xmlns` attribute.
```
<!--reference MyControls.dll-->
@using System
<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:mycontrols="clr-namespace:MyControls;assembly=MyControls"
...
    <mycontrols:OutlineTextControl Text="@Data.Title" StrokeThickness="1" Stroke="Black" Fill="White" />
```

It is also possible to use a `reference` comment with an [assembly name](https://docs.microsoft.com/en-us/dotnet/api/system.reflection.assemblyname.-ctor#System_Reflection_AssemblyName__ctor_System_String_), this is useful to reference standard .NET libraries:
```
<!--reference PresentationCore-->
@using System.Windows
```