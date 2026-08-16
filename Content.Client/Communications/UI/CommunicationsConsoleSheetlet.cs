using Content.Client.Stylesheets;
using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.StylesheetDefinitions;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Communications.UI;

/// <summary>
/// A sheetlet for the communications console, for the character limit labels.
/// </summary>
[Sheetlet(typeof(CommonStylesheetDefinition))]
public sealed class CommunicationsConsoleSheetlet<T> : ISheetlet<T>
    where T : IFontConfig, ICommunicationsConsoleConfig
{
    /// <inheritdoc/>
    public StyleRule[] GetRules(StylesheetDefinition sheet, T config)
    {
        return
        [
            E<Label>()
                .Class(ICommunicationsConsoleConfig.CharLimit)
                .Font(config.BaseFont.GetFont(8)),

            E<Label>()
                .Class(ICommunicationsConsoleConfig.CharLimitExceeded)
                .Font(config.BaseFont.GetFont(8))
                .FontColor(Color.Red),
        ];
    }
}

/// <summary>
/// Sheetlet config for the communication console.
/// </summary>
public interface ICommunicationsConsoleConfig : ISheetletConfig
{
    /// <summary>
    /// The name of a style class for char limit labels.
    /// </summary>
    const string CharLimit = "CommsConsoleCharLimit";

    /// <summary>
    /// The name of a style class for char limit labels when the reference text has exceeded its limit.
    /// </summary>
    const string CharLimitExceeded = "CommsConsoleCharLimitExceeded";
}
