using System.Diagnostics.CodeAnalysis;
using Robust.Client.UserInterface;

namespace Content.Client.Stylesheets;

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

    [Obsolete("Update to use SheetNanotrasen instead")]
    Stylesheet SheetNano { get; }

    [Obsolete("Update to use SheetSystem instead")]
    Stylesheet SheetSpace { get; }

    /// <summary>
    /// Resolve a stylesheet by name
    /// </summary>
    bool TryGetStylesheet(string name, [MaybeNullWhen(false)] out Stylesheet stylesheet);

    void Initialize();
}
