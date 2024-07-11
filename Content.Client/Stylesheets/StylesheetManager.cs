using System.Diagnostics;
using Content.Client.Stylesheets.Redux;
using Content.Client.Stylesheets.Redux.Stylesheets;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;

namespace Content.Client.Stylesheets
{
    public sealed class StylesheetManager : IStylesheetManager
    {
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;

        public Stylesheet SheetNano { get; private set; } = default!;
        public Stylesheet SheetInterface { get; private set; } = default!;
        public Stylesheet SheetSpace { get; private set; } = default!;

        public void Initialize()
        {
            var sawmill = _logManager.GetSawmill("style");
            sawmill.Debug("Initializing Stylesheets...");
            var sw = Stopwatch.StartNew();

            SheetNano = Init(new NanotrasenStylesheet(new PalettedStylesheet.NoConfig()));
            SheetInterface = Init(new InterfaceStylesheet(new PalettedStylesheet.NoConfig()));
            SheetSpace = new StyleSpace(_resourceCache).Stylesheet; // TODO: REMOVE

            _userInterfaceManager.Stylesheet = SheetNano;

            sawmill.Debug($"Initialized {_styleRuleCount} style rules in {sw.Elapsed}");
        }

        private int _styleRuleCount = 0;

        public Stylesheet Init(BaseStylesheet baseSheet)
        {
            _styleRuleCount += baseSheet.Stylesheet.Rules.Count;
            return baseSheet.Stylesheet;
        }
    }
}
