using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Stylesheets.Fonts;
using Content.Client.Stylesheets.Stylesheets;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Reflection;

namespace Content.Client.Stylesheets
{
    public sealed class StylesheetManager : IStylesheetManager, IPostInjectInit
    {
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IReflectionManager _reflection = default!;
        [Dependency] private readonly IDependencyCollection _deps = default!;
        [Dependency] private readonly IFontSelectionManager _fontSelection = default!;

        // TODO: REMOVE (obsolete; used to construct StyleNano/StyleSpace)
        [Dependency]
        private readonly IResourceCache _resCache = default!;

        private readonly Dictionary<Control, Func<IStylesheetAccessor, Stylesheet>> _controlStylesheetSubs = [];
        private readonly Dictionary<string, Stylesheet> _stylesheets = [];
        private readonly StylesheetAccessorImpl _accessor;

        private ISawmill _sawmill = null!;

        private int _styleRuleCount;
        private bool _initialized;

        private Stylesheet? _sheetNanotrasen;
        private Stylesheet? _sheetSystem;
        private Stylesheet? _sheetNanoLegacy;
        private Stylesheet? _sheetSpaceLegacy;

        public HashSet<Type> UnusedSheetlets { get; private set; } = [];

        [Obsolete("Access through UseStylesheet/IStylesheetAccessor instead")]
        public Stylesheet SheetNanotrasen => _accessor.SheetNanotrasen;

        [Obsolete("Access through UseStylesheet/IStylesheetAccessor instead")]
        public Stylesheet SheetSystem => _accessor.SheetSystem;

        [Obsolete("Update to use SheetNanotrasen instead")]
        public Stylesheet SheetNano =>
            _sheetNanoLegacy ?? throw new InvalidOperationException("Stylesheets not initialized yet!");

        [Obsolete("Update to use SheetSystem instead")]
        public Stylesheet SheetSpace =>
            _sheetSpaceLegacy ?? throw new InvalidOperationException("Stylesheets not initialized yet!");

        public StylesheetManager()
        {
            _accessor = new StylesheetAccessorImpl(this);
        }

        [Obsolete("Access through UseStylesheet/IStylesheetAccessor instead")]
        public bool TryGetStylesheet(string name, [MaybeNullWhen(false)] out Stylesheet stylesheet)
        {
            return _accessor.TryGetStylesheet(name, out stylesheet);
        }

        public void Initialize()
        {
            _fontSelection.OnFontChanged += OnFontChanged;
            _sawmill.Debug("Initializing Stylesheets...");
            var sw = Stopwatch.StartNew();

            RegenerateStylesheets();

            // warn about unused sheetlets
            if (UnusedSheetlets.Count > 0)
            {
                var sheetlets = UnusedSheetlets
                    .Take(5)
                    .Select(t => t.FullName ?? "<could not get FullName>");

                _sawmill.Error($"There are unloaded sheetlets: {string.Join(", ", sheetlets)}");
            }

            _sawmill.Debug($"Initialized {_styleRuleCount} style rules in {sw.Elapsed}");
            _initialized = true;
        }

        public void UseStylesheet(Control control, Func<IStylesheetAccessor, Stylesheet> getStylesheet)
        {
            _controlStylesheetSubs[control] = getStylesheet;
            control.Stylesheet = getStylesheet(_accessor);
        }

        public void StopStylesheet(Control control)
        {
            _controlStylesheetSubs.Remove(control);
        }

        private Stylesheet Init(BaseStylesheet baseSheet)
        {
            _stylesheets.Add(baseSheet.StylesheetName, baseSheet.Stylesheet);
            _styleRuleCount += baseSheet.Stylesheet.Rules.Count;
            return baseSheet.Stylesheet;
        }

        private void OnFontChanged(StandardFontType fontType)
        {
            if (!_initialized)
                return;

            RegenerateStylesheets();
        }

        private void RegenerateStylesheets()
        {
            _sawmill.Debug("Regenerating stylesheets...");

            // add all sheetlets to the hashset
            var tys = _reflection.FindTypesWithAttribute<CommonSheetletAttribute>();
            UnusedSheetlets = [..tys];

            _stylesheets.Clear();
            _sheetNanotrasen = Init(new NanotrasenStylesheet(new BaseStylesheet.NoConfig(), this, _deps));
            _sheetSystem = Init(new SystemStylesheet(new BaseStylesheet.NoConfig(), this, _deps));
#pragma warning disable CS0618 // Type or member is obsolete
            _sheetNanoLegacy = new StyleNano(_resCache).Stylesheet; // TODO: REMOVE (obsolete)
            _sheetSpaceLegacy = new StyleSpace(_resCache).Stylesheet; // TODO: REMOVE (obsolete)
#pragma warning restore CS0618 // Type or member is obsolete

            _userInterfaceManager.Stylesheet = _sheetNanotrasen;

            UpdateControls();
        }

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

        void IPostInjectInit.PostInject()
        {
            _sawmill = _logManager.GetSawmill("style");
        }

        private sealed class StylesheetAccessorImpl(StylesheetManager owner) : IStylesheetAccessor
        {
            public Stylesheet SheetNanotrasen => GetOrThrow(owner._sheetNanotrasen);
            public Stylesheet SheetSystem => GetOrThrow(owner._sheetSystem);

            public bool TryGetStylesheet(string name, [MaybeNullWhen(false)] out Stylesheet stylesheet)
            {
                if (!owner._initialized)
                    ThrowNotInitialized();

                return owner._stylesheets.TryGetValue(name, out stylesheet);
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
    }
}
