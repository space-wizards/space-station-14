using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

/// <summary>
/// Styles and helpers for labels that display a current and maximum character count.
/// </summary>
[CommonSheetlet]
public sealed class CharacterLimitSheetlet : Sheetlet<PalettedStylesheet>
{
    public const string StyleClass = "CharacterLimit";
    public const string ExceededStyleClass = "CharacterLimitExceeded";

    public static void UpdateLabel(Label label, int length, int maximum)
    {
        label.SetOnlyStyleClass(length > maximum
            ? ExceededStyleClass
            : StyleClass);
        label.Text = Loc.GetString("character-limit-label",
            ("count", length),
            ("max", maximum));
    }

    /// <inheritdoc/>
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        return
        [
            E<Label>()
                .Class(StyleClass)
                .Font(sheet.BaseFont.GetFont(8)),

            E<Label>()
                .Class(ExceededStyleClass)
                .Font(sheet.BaseFont.GetFont(8))
                .FontColor(Color.Red)
        ];
    }
}
