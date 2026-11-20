using Content.Client.Resources;
using Content.Client.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Communications.UI;

/// <summary>
/// A sheetlet for the communications console, for the character limit labels.
/// </summary>
[CommonSheetlet]
public sealed class CommunicationsConsoleSheetlet : Sheetlet<PalettedStylesheet>
{
    /// <summary>
    /// The name of a style class for char limit labels.
    /// </summary>
    public const string CharLimit = "CommsConsoleCharLimit";

    /// <summary>
    /// The name of a style class for char limit labels when the reference text has exceeded its limit.
    /// </summary>
    public const string CharLimitExceeded = "CommsConsoleCharLimitExceeded";

    /// <inheritdoc/>
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        return
        [
            E<Label>()
                .Class(CharLimit)
                .Font(sheet.BaseFont.GetFont(8)),

            E<Label>()
                .Class(CharLimitExceeded)
                .Font(sheet.BaseFont.GetFont(8))
                .FontColor(Color.Red)
        ];
    }
}
