using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.StylesheetFactories;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[Sheetlet(typeof(CommonStylesheetFactory))]
public sealed class PlaceholderSheetlet<T> : ISheetlet<T>
    where T : IPlaceholderConfig, IFontConfig
{
    public StyleRule[] GetRules(StylesheetFactory sheet, T config)
    {
        IPlaceholderConfig placeholderCfg = config;

        var placeholderBox = sheet.GetTexture(placeholderCfg.PlaceholderPath).IntoPatch(StyleBox.Margin.All, 19);
        placeholderBox.SetExpandMargin(StyleBox.Margin.All, -5);
        placeholderBox.Mode = StyleBoxTexture.StretchMode.Tile;

        return
        [
            E<Placeholder>()
                // ReSharper disable once AccessToStaticMemberViaDerivedType
                .Prop(Placeholder.StylePropertyPanel, placeholderBox),
            E<Label>()
                .Class(Placeholder.StyleClassPlaceholderText)
                .Font(config.BaseFont.GetFont(16))
                .FontColor(new Color(103, 103, 103, 128)), // TODO: fix hardcoded color
        ];
    }
}
