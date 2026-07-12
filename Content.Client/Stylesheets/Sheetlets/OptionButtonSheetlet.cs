using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.StylesheetFactories;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[Sheetlet(typeof(CommonStylesheetFactory))]
public sealed class OptionButtonSheetlet<T> : ISheetlet<T>
    where T : IIconConfig, IPaletteConfig
{
    public StyleRule[] GetRules(StylesheetFactory factory, T config)
    {
        IIconConfig iconCfg = config;

        var invertedTriangleTex = factory.GetTexture(iconCfg.InvertedTriangleIconPath);

        return
        [
            E<TextureRect>()
                .Class(OptionButton.StyleClassOptionTriangle)
                .Prop(TextureRect.StylePropertyTexture, invertedTriangleTex),
            E<Label>().Class(OptionButton.StyleClassOptionButton).AlignMode(Label.AlignMode.Center),
            E<PanelContainer>()
                .Class(OptionButton.StyleClassOptionsBackground)
                .Panel(new StyleBoxFlat(config.PrimaryPalette.Background)),
        ];
    }
}
