using System.Diagnostics.CodeAnalysis;
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
    [Obsolete("Access via UseStylesheet/IStylesheetAccessor instead")]
    Stylesheet SheetNanotrasen { get; }

    /// <summary>
    /// For heavily out-of-context UIs, such as admin UIs/debug UIs/changelog.
    /// </summary>
    [Obsolete("Access via UseStylesheet/IStylesheetAccessor instead")]
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
    /// Apply a stylesheet to a control and automatically subscribe to updates.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This will automatically update the stylesheet on the control if the backing stylesheet changed,
    /// for example due to user preferences.
    /// </para>
    /// <para>
    /// A call to <see cref="UseStylesheet"/> should always be paired with a call to <see cref="StopStylesheet"/>,
    /// otherwise memory leaks will ensue! The best way to do this is to call <see cref="UseStylesheet"/> in
    /// <see cref="Control.EnteredTree"/>, and call <see cref="StopStylesheet"/> in <see cref="Control.ExitedTree"/>.
    /// </para>
    /// <para>
    /// If this method gets called twice on the same control, it will simply replace the previous
    /// <paramref name="getStylesheet"/> method. In this scenario, <see cref="StopStylesheet"/> does <b>not</b> need to
    /// be called another time for cleanup, in this scenario.
    /// </para>
    /// </remarks>
    /// <param name="control">The control to apply the stylesheet to.</param>
    /// <param name="getStylesheet">
    /// A function used to select the stylesheet from the <see cref="IStylesheetAccessor"/>.
    /// </param>
    void UseStylesheet(Control control, Func<IStylesheetAccessor, Stylesheet?> getStylesheet);

    /// <summary>
    /// Stop stylesheet update subscription from <see cref="UseStylesheet"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This does not (currently) unset the stylesheet on <paramref name="control"/>, as a performance optimization.
    /// Do not rely on this.
    /// </para>
    /// </remarks>
    /// <param name="control">The control to unsubscribe.</param>
    void StopStylesheet(Control control);

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
    /// System stylesheet: used for OOC UIs.
    /// </summary>
    Stylesheet SheetSystem { get; }

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
