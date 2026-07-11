using Content.Client.Examine;
using Content.Client.Stylesheets.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets.Hud;

[Sheetlet(typeof(CommonStylesheetFactory))]
public sealed class ExamineButtonSheetlet<T> : ISheetlet<T>
    where T : ISheetletConfig
{
    // Examine button colors
    // TODO: FIX!!
    private static readonly Color ExamineButtonColorContext = Color.Transparent;
    private static readonly Color ExamineButtonColorContextHover = Color.DarkSlateGray;
    private static readonly Color ExamineButtonColorContextPressed = Color.LightSlateGray;
    private static readonly Color ExamineButtonColorContextDisabled = Color.FromHex("#5A5A5A");

    public StyleRule[] GetRules(StylesheetFactory factory, T config)
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
                .Prop(Control.StylePropertyModulateSelf, ExamineButtonColorContext),
            E<ExamineButton>()
                .Class(ExamineButton.StyleClassExamineButton)
                .PseudoHovered()
                .Prop(Control.StylePropertyModulateSelf, ExamineButtonColorContextHover),
            E<ExamineButton>()
                .Class(ExamineButton.StyleClassExamineButton)
                .PseudoPressed()
                .Prop(Control.StylePropertyModulateSelf, ExamineButtonColorContextPressed),
            E<ExamineButton>()
                .Class(ExamineButton.StyleClassExamineButton)
                .PseudoDisabled()
                .Prop(Control.StylePropertyModulateSelf, ExamineButtonColorContextDisabled),
        ];
    }
}
