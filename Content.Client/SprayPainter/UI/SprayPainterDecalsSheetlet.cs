using Content.Client.Stylesheets;
using Content.Client.Stylesheets.SheetletConfigs;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.SprayPainter.UI;

[CommonSheetlet]
public sealed class SprayPainterDecalsSheetlet<T> : Sheetlet<T> where T : IButtonConfig
{
    /// <summary>
    /// The SprayPainterDecals Control modulates the color of the Control containing
    /// all the decals. However, to highlight the currently-selected decal, we don't
    /// want to inherit this modulation. This StyleBox temporarily disables that
    /// modulation and forwards to another StyleBox
    /// </summary>
    private sealed class OverrideModulationStyleBox(StyleBox baseBox) : StyleBox
    {
        private readonly StyleBox _baseBox = baseBox;

        protected override void DoDraw(DrawingHandleScreen handle, UIBox2 box, float uiScale)
        {
            var oldModulation = handle.Modulate;
            handle.Modulate = Color.White;
            _baseBox.Draw(handle, box, uiScale);
            handle.Modulate = oldModulation;
        }
    }

    public override StyleRule[] GetRules(T sheet, object config)
    {
        var noBackground = new StyleBoxFlat { BackgroundColor = Color.Transparent };
        var backgroundColor = sheet.ButtonPalette.BackgroundLight;
        var highlighted = new OverrideModulationStyleBox(new StyleBoxFlat() { BackgroundColor = backgroundColor });

        return
        [
            E<Button>()
                .Class(SprayPainterDecals.StyleClassDecalButton)
                .Box(noBackground),
            E<Button>()
                .Class(SprayPainterDecals.StyleClassDecalButton)
                .PseudoPressed()
                .Box(highlighted),
        ];
    }
}
