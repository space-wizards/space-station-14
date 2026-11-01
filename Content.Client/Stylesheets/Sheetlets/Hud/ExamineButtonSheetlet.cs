using Content.Client.Examine;
using Content.Client.Stylesheets.Stylesheets;
using Content.Client.Stylesheets.Palette;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets.Hud;

[CommonSheetlet]
public sealed class ExamineButtonSheetlet : Sheetlet<NanotrasenStylesheet>
{
    public override StyleRule[] GetRules(NanotrasenStylesheet sheet, object config)
    {
        var buttonContext = new StyleBoxTexture { Texture = Texture.White };

        return
        [
            E<ExamineButton>()
                .Class(ExamineButton.StyleClassExamineButton)
                .Prop(ContainerButton.StylePropertyStyleBox, buttonContext),
            E<ExamineButton>()
                .Class(ExamineButton.StyleClassExamineButton)
                .PseudoNormal()
                .Prop(Control.StylePropertyModulateSelf, Palettes.AlphaModulate.Base.WithAlpha(0f)),
            E<ExamineButton>()
                .Class(ExamineButton.StyleClassExamineButton)
                .PseudoHovered()
                .Prop(Control.StylePropertyModulateSelf, Palettes.Emerald.Element),
            E<ExamineButton>()
                .Class(ExamineButton.StyleClassExamineButton)
                .PseudoPressed()
                .Prop(Control.StylePropertyModulateSelf, Palettes.Emerald.PressedElement),
            E<ExamineButton>()
                .Class(ExamineButton.StyleClassExamineButton)
                .PseudoDisabled()
                .Prop(Control.StylePropertyModulateSelf, sheet.PrimaryPalette.BackgroundDark),
        ];
    }
}
