using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.StylesheetFactories;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[Sheetlet(typeof(CommonStylesheetFactory))]
public sealed class ConfirmButtonSheetlet<T> : ISheetlet<T>
    where T : IPaletteConfig
{
    public StyleRule[] GetRules(StylesheetFactory factory, T config)
    {
        return
        [
            E<ConfirmButton>()
                .Pseudo(ConfirmButton.ConfirmPrefix + ContainerButton.StylePseudoClassNormal)
                .Prop(Control.StylePropertyModulateSelf, config.NegativePalette.Element),

            E<ConfirmButton>()
                .Pseudo(ConfirmButton.ConfirmPrefix + ContainerButton.StylePseudoClassHover)
                .Prop(Control.StylePropertyModulateSelf, config.NegativePalette.HoveredElement),

            E<ConfirmButton>()
                .Pseudo(ConfirmButton.ConfirmPrefix + ContainerButton.StylePseudoClassPressed)
                .Prop(Control.StylePropertyModulateSelf, config.NegativePalette.PressedElement),

            E<ConfirmButton>()
                .Pseudo(ConfirmButton.ConfirmPrefix + ContainerButton.StylePseudoClassDisabled)
                .Prop(Control.StylePropertyModulateSelf, config.NegativePalette.DisabledElement),
        ];
    }
}
