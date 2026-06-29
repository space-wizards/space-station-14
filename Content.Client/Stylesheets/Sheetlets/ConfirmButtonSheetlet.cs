using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Stylesheets;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.UserInterface.Controls;

[CommonSheetlet]
public sealed class ConfirmButtonSheetlet : Sheetlet<NanotrasenStylesheet>
{
    public override StyleRule[] GetRules(NanotrasenStylesheet sheet, object config)
    {
        return [
            E<ConfirmButton>()
                .Pseudo(ConfirmButton.ConfirmPrefix + ContainerButton.StylePseudoClassNormal)
                .Prop(Control.StylePropertyModulateSelf, sheet.NegativePalette.Element),

            E<ConfirmButton>()
                .Pseudo(ConfirmButton.ConfirmPrefix + ContainerButton.StylePseudoClassHover)
                .Prop(Control.StylePropertyModulateSelf, sheet.NegativePalette.HoveredElement),

            E<ConfirmButton>()
                .Pseudo(ConfirmButton.ConfirmPrefix + ContainerButton.StylePseudoClassPressed)
                .Prop(Control.StylePropertyModulateSelf, sheet.NegativePalette.PressedElement),

            E<ConfirmButton>()
                .Pseudo(ConfirmButton.ConfirmPrefix + ContainerButton.StylePseudoClassDisabled)
                .Prop(Control.StylePropertyModulateSelf, sheet.NegativePalette.DisabledElement),
        ];
    }
}
