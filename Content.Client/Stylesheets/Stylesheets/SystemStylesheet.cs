using System.Linq;
using Content.Client.Stylesheets.Fonts;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.StylesheetHelpers;


namespace Content.Client.Stylesheets.Stylesheets;

[Virtual]
public partial class SystemStylesheet : CommonStylesheet
{
    public override string StylesheetName => "System";

    public override NotoFontFamilyStack BaseFont { get; } // TODO: NotoFontFamilyStack is temporary

    public override Dictionary<Type, ResPath[]> Roots => new()
    {
        { typeof(TextureResource), [] },
    };

    private const int PrimaryFontSize = 12;
    private const int FontSizeStep = 2;

    // for some GOD FORSAKEN REASON if I use a collection expression here it throws a sandbox error
    // Thanks ReSharper, this was very fun to find in the ~40 files I last committed
    // ReSharper disable once UseCollectionExpression
    private readonly List<(string?, int)> _commonFontSizes = new()
    {
        (null, PrimaryFontSize),
        (StyleClass.FontSmall, PrimaryFontSize - FontSizeStep),
        (StyleClass.FontLarge, PrimaryFontSize + FontSizeStep),
    };

    public SystemStylesheet(object config, StylesheetManager man) : base(config)
    {
        BaseFont = new NotoFontFamilyStack(ResCache);
        var rules = new[]
        {
            // Set up important rules that need to go first.
            GetRulesForFont(null, BaseFont, _commonFontSizes),
            // Set up our core rules.
            [
                // Declare the default font.
                Element().Prop(Label.StylePropertyFont, BaseFont.GetFont(PrimaryFontSize)),
            ],
            // Finally, load all the other sheetlets.
            GetAllSheetletRules<PalettedStylesheet, CommonSheetletAttribute>(man),
            GetAllSheetletRules<SystemStylesheet, CommonSheetletAttribute>(man),
        };

        Stylesheet = new Stylesheet(rules.SelectMany(x => x).ToArray());
    }
}
