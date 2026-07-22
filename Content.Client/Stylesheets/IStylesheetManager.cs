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
    /// <remarks>
    /// <para>
    /// This will let you automatically update the stylesheet/fonts on the control if the backing stylesheet changed,
    /// for example due to user preferences.
    /// </para>
    /// <para>
    /// A call to <see cref="UseStyle"/> should always be paired with a call to <see cref="StopStyle"/>,
    /// otherwise memory leaks will ensue! The best way to do this is to call <see cref="UseStyle"/> in
    /// <see cref="Control.EnteredTree"/>, and call <see cref="StopStyle"/> in <see cref="Control.ExitedTree"/>.
    /// </para>
    /// </remarks>
    /// <param name="action">
    /// A function used to select style properties (e.g. stylesheet, font) from <see cref="IStylesheetAccessor"/>.
    /// </param>
    void UseStyle(Action<IStylesheetAccessor> action);

    /// <summary>
    /// Unsubscribe from style changes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This will not return you to defaults. If you wish to change a style, call UseStyle again with a different Action.
    /// </para>
    /// </remarks>
    /// <param name="action">The action to unsubscribe.</param>
    void StopStyle(Action<IStylesheetAccessor> action);

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
