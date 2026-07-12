using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Content.Client.Stylesheets.StylesheetFactories;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;

namespace Content.Client.Stylesheets;

/// <inheritdoc cref="IStylesheetManager"/>
public sealed partial class StylesheetManager : IStylesheetManager, IPostInjectInit
{
    [Dependency] private ILogManager _logManager = default!;
    [Dependency] private IUserInterfaceManager _userInterfaceManager = default!;

    // TODO: REMOVE (obsolete; used to construct StyleNano/StyleSpace)
    [Dependency] private IResourceCache _resCache = default!;

    private readonly Dictionary<Control, Func<IStylesheetAccessor, Stylesheet>> _controlStylesheetSubs = [];
    private readonly Dictionary<string, Stylesheet> _stylesheets = [];
    private readonly StylesheetAccessorImpl _accessor;

    private ISawmill _sawmill = null!;

    private bool _initialized;

    private Stylesheet? _sheetNanotrasen;
    private Stylesheet? _sheetSystem;
    private Stylesheet? _sheetNanoLegacy;
    private Stylesheet? _sheetSpaceLegacy;

    public StylesheetManager()
    {
        _accessor = new StylesheetAccessorImpl(this);
    }

    public void Initialize()
    {
        _sawmill.Debug("Initializing Stylesheets...");
        var sw = Stopwatch.StartNew();

        RegenerateStylesheets();

        _sawmill.Debug(
            $"Initialized {_sheetNanotrasen?.Rules.Count + _sheetSystem?.Rules.Count} style rules in {sw.Elapsed}");
        _initialized = true;
    }

    /// <inheritdoc/>
    public void UseStylesheet(Control control, Func<IStylesheetAccessor, Stylesheet> getStylesheet)
    {
        _controlStylesheetSubs[control] = getStylesheet;
        control.Stylesheet = getStylesheet(_accessor);
    }

    /// <inheritdoc/>
    public void StopStylesheet(Control control)
    {
        // Not unsetting the stylesheet here (which would make it resolve to the default) for performance reasons.
        _controlStylesheetSubs.Remove(control);
    }

    private void RegenerateStylesheets()
    {
        _sawmill.Debug("Regenerating stylesheets...");

        _stylesheets.Clear();

        // TODO: these factories can be saved/cached, but it's so infrequently used that it doesn't feel necessary.
        _sheetNanotrasen = new NanotrasenStylesheetFactory().Build();
        _sheetSystem = new SystemStylesheetFactory().Build();

#pragma warning disable CS0618 // Type or member is obsolete
        _sheetNanoLegacy = new StyleNano(_resCache).Stylesheet;
        _sheetSpaceLegacy = new StyleSpace(_resCache).Stylesheet;
#pragma warning restore CS0618 // Type or member is obsolete

        _stylesheets.Add("Nanotrasen", _sheetNanotrasen);
        _stylesheets.Add("System", _sheetSystem);

        // Default stylesheet (which will automatically propogate and update any UIs without a specific Stylesheet set)
        _userInterfaceManager.Stylesheet = _sheetNanotrasen;

        UpdateControls();
    }

    /// <summary>
    /// Updates all controls that have specifically selected a stylesheet.
    /// </summary>
    private void UpdateControls()
    {
        foreach (var (control, getStylesheet) in _controlStylesheetSubs)
        {
            try
            {
                control.Stylesheet = getStylesheet(_accessor);
            }
            catch (Exception e)
            {
                _sawmill.Error($"Caught exception while updating stylesheets on controls! {e}");
            }
        }
    }

    /// <inheritdoc/>
    void IPostInjectInit.PostInject()
    {
        _sawmill = _logManager.GetSawmill("style");
    }

    /// <inheritdoc/>
    private sealed class StylesheetAccessorImpl(StylesheetManager owner) : IStylesheetAccessor
    {
        /// <inheritdoc/>
        public Stylesheet SheetNanotrasen => GetOrThrow(owner._sheetNanotrasen);

        /// <inheritdoc/>
        public Stylesheet SheetSystem => GetOrThrow(owner._sheetSystem);

        /// <inheritdoc/>
        public bool TryGetStylesheet(string name, [MaybeNullWhen(false)] out Stylesheet stylesheet)
        {
            if (!owner._initialized)
                ThrowNotInitialized();

            return owner._stylesheets.TryGetValue(name, out stylesheet);
        }

        /// <inheritdoc/>
        public Stylesheet GetStylesheetOrDefault(string name, Stylesheet defaultStylesheet)
        {
            if (TryGetStylesheet(name, out var stylesheet))
                return stylesheet;
            else
            {
                owner._sawmill.Warning($"Failed to resolve stylesheet {name}");
                return defaultStylesheet;
            }
        }

        private static Stylesheet GetOrThrow(Stylesheet? sheet)
        {
            return sheet ?? ThrowNotInitialized();
        }

        [DoesNotReturn]
        private static Stylesheet ThrowNotInitialized()
        {
            throw new InvalidOperationException("Stylesheets not initialized yet!");
        }
    }

    #region Obsolete

    /// <inheritdoc/>
    [Obsolete("Access through UseStylesheet/IStylesheetAccessor instead")]
    public Stylesheet SheetNanotrasen => _accessor.SheetNanotrasen;

    /// <inheritdoc/>
    [Obsolete("Access through UseStylesheet/IStylesheetAccessor instead")]
    public Stylesheet SheetSystem => _accessor.SheetSystem;

    /// <inheritdoc/>
    [Obsolete("Update to use SheetNanotrasen instead")]
    public Stylesheet SheetNano =>
        _sheetNanoLegacy ?? throw new InvalidOperationException("Stylesheets not initialized yet!");

    /// <inheritdoc/>
    [Obsolete("Update to use SheetSystem instead")]
    public Stylesheet SheetSpace =>
        _sheetSpaceLegacy ?? throw new InvalidOperationException("Stylesheets not initialized yet!");

    /// <inheritdoc/>
    [Obsolete("Access through UseStylesheet/IStylesheetAccessor instead")]
    public bool TryGetStylesheet(string name, [MaybeNullWhen(false)] out Stylesheet stylesheet)
    {
        return _accessor.TryGetStylesheet(name, out stylesheet);
    }

    #endregion
}
