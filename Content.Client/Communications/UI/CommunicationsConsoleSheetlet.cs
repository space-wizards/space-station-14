using Content.Client.Resources;
using Content.Client.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Communications.UI;

/// <summary>
/// A sheetlet for the communications console,
/// </summary>
[CommonSheetlet]
public sealed class CommunicationsConsoleSheetlet : Sheetlet<PalettedStylesheet>
{
    /// <inheritdoc/>
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        return
        [
            E<RichTextLabel>()
                .Class("CommsConsoleCharLimit")
                .Font(sheet.BaseFont.GetFont(8))
        ];
    }
}
