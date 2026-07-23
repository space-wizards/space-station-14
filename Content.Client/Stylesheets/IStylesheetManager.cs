using System.Diagnostics.CodeAnalysis;
using Content.Client.Stylesheets.SheetletConfigs;
using Robust.Client.UserInterface;

namespace Content.Client.Stylesheets;

/// <summary>
/// Creates and provides access to stylesheets.
/// </summary>
public interface IStylesheetManager
{
    #region Obsolete APIs

    /// <summary>
    /// Default UI style, used for in-game/in-context UIs, but used basically everywhere regardless.
    /// </summary>
    [Obsolete("Access via UseStyle instead")]
    Stylesheet SheetNanotrasen { get; }

    /// <summary>
    /// For heavily out-of-context UIs, such as admin UIs/debug UIs/changelog.
    /// </summary>
    [Obsolete("Access via UseStyle instead")]
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
    [Obsolete("Access via UseStylesheet/IStylesheetAccessor instead")]
    bool TryGetStylesheet(string name, [MaybeNullWhen(false)] out Stylesheet stylesheet);

    #endregion

    /// <summary>
    /// Subscribe to style changes.
    /// </summary>
    /// <para>
    /// Callers are immediately invoked on subscription and invoked each time the stylesheets/fonts change.
    /// </para>
    /// <para>
    /// You must then unsubscribe from the same delegate later, otherwise there'll be a memory leak!
    /// It's recommended to subscribe in <see cref="Control.EnteredTree"/> and unsubscribe in <see cref="Control.ExitedTree"/> to mitigate these issues.
    /// </para>
    event Action<IStylesheetAccessor> StyleChanged;

    /// <summary>
    /// Initialize the stylesheet manager.
    /// </summary>
    void Initialize();
}

/// <summary>
/// Provides access to stylesheets on the <see cref="IStylesheetManager"/>.
/// </summary>
public interface IStylesheetAccessor
{
    /// <summary>
    /// Nanotrasen style sheet: should be used for IC UIs like machines.
    /// </summary>
    /// <remarks>
    /// Is currently default for legacy reasons.
    /// </remarks>
    Stylesheet SheetNanotrasen { get; }

    /// <summary>
    /// Fonts used by the Nanotrasen stylesheet.
    /// </summary>
    IFontConfig FontNanotrasen { get; }

    /// <summary>
    /// System stylesheet: used for OOC UIs.
    /// </summary>
    Stylesheet SheetSystem { get; }

    /// <summary>
    /// Fonts used by the System stylesheet.
    /// </summary>
    IFontConfig FontSystem { get; }

    /// <summary>
    /// Gets a stylesheet, or prints an error and falls back to [].
    /// </summary>
    /// <param name="name">Stylesheet name</param>
    /// <returns>The stylesheet, or null if found</returns>
    Stylesheet? GetStylesheet(string name);

    /// <summary>
    /// Try to get a stylesheet by name.
    /// </summary>
    bool TryGetStylesheet(string name, [NotNullWhen(true)] out Stylesheet? stylesheet);

    /// <summary>
    /// Get a stylesheet, or fallback to the provided default.
    /// </summary>
    Stylesheet GetStylesheetOrDefault(string name, Stylesheet defaultStylesheet);
}
