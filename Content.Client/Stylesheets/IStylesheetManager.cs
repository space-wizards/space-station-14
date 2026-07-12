using System.Diagnostics.CodeAnalysis;
using Robust.Client.UserInterface;

namespace Content.Client.Stylesheets;

/// <summary>
/// Creates and provides access to stylesheets.
/// </summary>
public interface IStylesheetManager
{
    /// <summary>
    /// Default UI style, used for in-game/in-context UIs, but used basically everywhere anyways.
    /// </summary>
    Stylesheet SheetNanotrasen { get; }

    /// <summary>
    /// For heavily out-of-context UIs, such as admin UIs/debug UIs/changelog.
    /// </summary>
    Stylesheet SheetSystem { get; }

    /// <summary>
    /// Legacy StyleNano.
    /// </summary>
    [Obsolete("Update to use SheetNanotrasen instead")]
    Stylesheet SheetNano { get; }

    /// <summary>
    /// Legacy StyleSpace.
    /// </summary>
    [Obsolete("Update to use SheetSystem instead")]
    Stylesheet SheetSpace { get; }

    /// <summary>
    /// Resolve a stylesheet by name
    /// </summary>
    bool TryGetStylesheet(string name, [MaybeNullWhen(false)] out Stylesheet stylesheet);

    /// <summary>
    /// Initalize the stylesheet manager.
    /// </summary>
    void Initialize();
}
