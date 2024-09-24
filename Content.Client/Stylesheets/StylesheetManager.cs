using System.Diagnostics;
using Content.Client.Stylesheets.Redux;
using Content.Client.Stylesheets.Redux.Stylesheets;
using Robust.Client.UserInterface;

namespace Content.Client.Stylesheets
{
    public sealed class StylesheetManager : IStylesheetManager
    {
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

        public Stylesheet SheetNanotransen { get; private set; } = default!;
        public Stylesheet SheetSystem { get; private set; } = default!;

        // obsolete, TODO(maybe): bring back normal StyleNano.cs / StyleSpace.cs? for easier merging.
        public Stylesheet SheetNano { get; } = default!;
        public Stylesheet SheetSpace { get; } = default!;

        public Dictionary<string, Stylesheet> Stylesheets { get; private set; } = default!;

        public void Initialize()
        {
            var sawmill = _logManager.GetSawmill("style");
            sawmill.Debug("Initializing Stylesheets...");
            var sw = Stopwatch.StartNew();

            Stylesheets = new Dictionary<string, Stylesheet>();

            SheetNanotransen = Init("Nanotransen", new NanotrasenStylesheet(new BaseStylesheet.NoConfig()));
            SheetSystem = Init("Interface", new SystemStylesheet(new BaseStylesheet.NoConfig()));

            _userInterfaceManager.Stylesheet = SheetNanotransen;

            sawmill.Debug($"Initialized {_styleRuleCount} style rules in {sw.Elapsed}");
        }

        private int _styleRuleCount;

        public Stylesheet Init(string name, BaseStylesheet baseSheet)
        {
            Stylesheets.Add(name, baseSheet.Stylesheet);
            _styleRuleCount += baseSheet.Stylesheet.Rules.Count;
            return baseSheet.Stylesheet;
        }
    }
}
