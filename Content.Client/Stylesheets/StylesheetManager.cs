using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.StylesheetDefinitions;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace Content.Client.Stylesheets;

/// <inheritdoc cref="IStylesheetManager"/>
public sealed partial class StylesheetManager : IStylesheetManager, IPostInjectInit
{
    [Dependency] private ILogManager _logManager = default!;
    [Dependency] private IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private IResourceCache _resCache = default!;

    private readonly List<Action<IStylesheetAccessor>> _subscriptions = [];
    private readonly Dictionary<string, Stylesheet> _stylesheets = [];
    private readonly StylesheetAccessorImpl _accessor;

    private ISawmill _sawmill = null!;

    private bool _initialized;

    private Stylesheet? _sheetNanotrasen;
    private Stylesheet? _sheetSystem;
    private Stylesheet? _sheetNanoLegacy;
    private Stylesheet? _sheetSpaceLegacy;

    private IFontConfig? _fontNanotrasen;
    private IFontConfig? _fontSystem;

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
    public void UseStyle(Action<IStylesheetAccessor> action)
    {
        DebugTools.Assert(!_subscriptions.Contains(action), "Attempted to subscribe the same style action twice.");
        _subscriptions.Add(action);

        try
        {
            action(_accessor);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Caught exception while updating styles on controls! {e}");
        }
    }

    /// <inheritdoc/>
    public void StopStyle(Action<IStylesheetAccessor> action)
    {
        DebugTools.Assert(_subscriptions.Contains(action),
            "Attempted to unsubscribe from a style action that was not subscribed.");
        _subscriptions.Remove(action);
    }

    private void RegenerateStylesheets()
    {
        _sawmill.Debug("Regenerating stylesheets...");

        _stylesheets.Clear();

        // TODO: these definitions can be saved/cached, but it's so infrequently used that it doesn't feel necessary.
        var nano = new NanotrasenStylesheetDefinition();
        _sheetNanotrasen = nano.Build();
        _fontNanotrasen = nano;

        var system = new SystemStylesheetDefinition();
        _sheetSystem = system.Build();
        _fontSystem = system;

#pragma warning disable CS0618
        _sheetNanoLegacy = new StyleNano(_resCache).Stylesheet;
        _sheetSpaceLegacy = new StyleSpace(_resCache).Stylesheet;
#pragma warning restore CS0618

        _stylesheets.Add("Nanotrasen", _sheetNanotrasen);
        _stylesheets.Add("System", _sheetSystem);

        // Default stylesheet (which will automatically propagate and update any UIs without a specific Stylesheet set)
        _userInterfaceManager.Stylesheet = _sheetNanotrasen;

        UpdateStyles();
    }

    /// <summary>
    /// Updates all controls that have subscribed to style changes.
    /// </summary>
    private void UpdateStyles()
    {
        foreach (var sub in _subscriptions)
        {
            try
            {
                sub.Invoke(_accessor);
            }
            catch (Exception e)
            {
                _sawmill.Error($"Caught exception while updating styles on controls! {e}");
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
        public IFontConfig FontNanotrasen => GetOrThrow(owner._fontNanotrasen);

        /// <inheritdoc/>
        public Stylesheet SheetSystem => GetOrThrow(owner._sheetSystem);

        /// <inheritdoc/>
        public IFontConfig FontSystem => GetOrThrow(owner._fontSystem);

        /// <inheritdoc/>
        public Stylesheet? GetStylesheet(string name)
        {
            if (TryGetStylesheet(name, out var stylesheet))
                return stylesheet;

            owner._sawmill.Error($"Failed to resolve stylesheet {name}");
            return null;
        }

        /// <inheritdoc/>
        public bool TryGetStylesheet(string name, [NotNullWhen(true)] out Stylesheet? stylesheet)
        {
            if (!owner._initialized)
                ThrowNotInitialized<Stylesheet>();

            return owner._stylesheets.TryGetValue(name, out stylesheet);
        }

        /// <inheritdoc/>
        public Stylesheet GetStylesheetOrDefault(string name, Stylesheet defaultStylesheet)
        {
            if (TryGetStylesheet(name, out var stylesheet))
                return stylesheet;

            owner._sawmill.Debug($"Failed to resolve stylesheet {name}");
            return defaultStylesheet;
        }

        /// <summary>
        /// Gets the non-null object, or throws a not-initialized exception.
        /// </summary>
        /// <param name="sheet">The style object</param>
        /// <returns>The style object, or does not return</returns>
        /// <exception cref="InvalidOperationException">Style object not initialized yet.</exception>
        private static T GetOrThrow<T>(T? sheet)
        {
            return sheet ?? ThrowNotInitialized<T>();
        }

        /// <summary>
        /// Throws a not initialized error.
        /// </summary>
        /// <returns>Does not return</returns>
        /// <exception cref="InvalidOperationException">Style object not initialized yet.</exception>
        [DoesNotReturn]
        private static T ThrowNotInitialized<T>()
        {
            throw new InvalidOperationException("Stylesheets not initialized yet!");
        }
    }

    #region Obsolete

    /// <inheritdoc/>
    [Obsolete("Access through UseStyle instead")]
    public Stylesheet SheetNanotrasen => _accessor.SheetNanotrasen;

    /// <inheritdoc/>
    [Obsolete("Access through UseStyle instead")]
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
    [Obsolete("Access through UseStyle instead")]
    public bool TryGetStylesheet(string name, [MaybeNullWhen(false)] out Stylesheet stylesheet)
    {
        return _accessor.TryGetStylesheet(name, out stylesheet);
    }

    #endregion
}
