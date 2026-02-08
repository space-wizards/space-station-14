using System.Diagnostics.CodeAnalysis;
using Robust.Client.UserInterface;

namespace Content.Client.Stylesheets;

public interface IStylesheetManager
{
    /// Nanotrasen styles: the default style! Use this for most UIs
    [Obsolete("Access through UseStylesheet/IStylesheetAccessor instead")]
    Stylesheet SheetNanotrasen { get; }

    ///
    /// System styles: use this for any admin / debug menus, and any odds and ends (like the changelog for some reason)
    ///
    [Obsolete("Access through UseStylesheet/IStylesheetAccessor instead")]
    Stylesheet SheetSystem { get; }

    [Obsolete("Update to use SheetNanotrasen instead")]
    Stylesheet SheetNano { get; }

    [Obsolete("Update to use SheetSystem instead")]
    Stylesheet SheetSpace { get; }

    /// get a stylesheet by name
    [Obsolete("Access through UseStylesheet/IStylesheetAccessor instead")]
    public bool TryGetStylesheet(string name, [MaybeNullWhen(false)]  out Stylesheet stylesheet);

    void Initialize();

    ///
    /// Sheetlets marked with CommonSheetlet that have not satisfied the type constraints of any stylesheet
    ///
    public HashSet<Type> UnusedSheetlets { get; }

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
    void UseStylesheet(Control control, Func<IStylesheetAccessor, Stylesheet> getStylesheet);

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
    /// Get a stylesheet by name.
    /// </summary>
    public bool TryGetStylesheet(string name, [MaybeNullWhen(false)] out Stylesheet stylesheet);
}
