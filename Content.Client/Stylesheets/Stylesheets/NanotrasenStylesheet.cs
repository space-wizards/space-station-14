using System.Linq;
using Content.Client.Stylesheets.Fonts;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Content.Client.Stylesheets.Stylesheets;

[Virtual]
public sealed partial class NanotrasenStylesheet : CommonStylesheet
{
    public override string StylesheetName => "Nanotrasen";

    [Obsolete("Use the newer font stack instead")]
    public override NotoFontFamilyStack BaseFont { get; } // TODO: NotoFontFamilyStack is temporary

    public static readonly ResPath TextureRoot = new("/Textures/Interface/Nano");

    public override Dictionary<Type, ResPath[]> Roots => new()
    {
        { typeof(TextureResource), [TextureRoot] },
    };

    public const int PrimaryFontSize = 12;
    private const int FontSizeStep = 2;

    // why? see InterfaceStylesheet.cs
    // ReSharper disable once UseCollectionExpression
    private readonly List<(string?, int)> _commonFontSizes = new()
    {
        (null, PrimaryFontSize),
        (StyleClass.FontSmall, PrimaryFontSize - FontSizeStep),
        (StyleClass.FontLarge, PrimaryFontSize + FontSizeStep),
    };

    public NanotrasenStylesheet(object config, StylesheetManager man, IDependencyCollection deps) : base(config, deps)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        BaseFont = new NotoFontFamilyStack(ResCache);
#pragma warning restore CS0618 // Type or member is obsolete
        var rules = new[]
        {
            // Set up important rules that need to go first.
            GetRulesForFont(null, StandardFontType.Main, _commonFontSizes),
            // Set up our core rules.
            [
                // Declare the default font.
                Element().Prop(Label.StylePropertyFont, Fonts.GetFont(StandardFontType.Main, PrimaryFontSize)),
                // Branding.
                Element<TextureRect>()
                    .Class("NTLogoDark")
                    .Prop(TextureRect.StylePropertyTexture, GetTexture(new ResPath("ntlogo.svg.png")))
                    .Prop(Control.StylePropertyModulateSelf, Color.FromHex("#757575")),
            ],
            // Finally, load all the other sheetlets.
            GetAllSheetletRules<PalettedStylesheet, CommonSheetletAttribute>(man),
            GetAllSheetletRules<NanotrasenStylesheet, CommonSheetletAttribute>(man),
        };

        Stylesheet = new Stylesheet(rules.SelectMany(x => x).ToArray());
    }

    [Obsolete("Pass in IDependencyCollection directly")]
    public NanotrasenStylesheet(object config, StylesheetManager man)
        : this(config, man, IoCManager.Resolve<IDependencyCollection>())
    {
    }
}
