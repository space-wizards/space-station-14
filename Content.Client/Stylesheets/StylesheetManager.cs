using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Stylesheets.Stylesheets;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Reflection;

namespace Content.Client.Stylesheets
{
    public sealed partial class StylesheetManager : IStylesheetManager
    {
        [Dependency] private ILogManager _logManager = default!;
        [Dependency] private IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private IReflectionManager _reflection = default!;

        // TODO: REMOVE (obsolete; used to construct StyleNano/StyleSpace)
        [Dependency] private IResourceCache _resCache = default!;

        public Stylesheet SheetNanotrasen { get; private set; } = default!;
        public Stylesheet SheetSystem { get; private set; } = default!;

        [Obsolete("Update to use SheetNanotrasen instead")]
        public Stylesheet SheetNano { get; private set; } = default!;

        [Obsolete("Update to use SheetSystem instead")]
        public Stylesheet SheetSpace { get; private set; } = default!;

        private Dictionary<string, Stylesheet> Stylesheets { get; set; } = [];

        public void Initialize()
        {
            var sawmill = _logManager.GetSawmill("style");
            sawmill.Debug("Initializing Stylesheets...");
            var sw = Stopwatch.StartNew();

            Stylesheets = new Dictionary<string, Stylesheet>();

            SheetNanotrasen = new NanotrasenStylesheetFactory().Build();
            Stylesheets.Add("Nanotrasen", SheetNanotrasen);

            SheetSystem = new SystemStylesheetFactory().Build();
            Stylesheets.Add("System", SheetSystem);

#pragma warning disable CS0618 // Type or member is obsolete
            // NOTE: Please delete.
            SheetNano = new StyleNano(_resCache).Stylesheet;
            SheetSpace = new StyleSpace(_resCache).Stylesheet;
#pragma warning restore CS0618 // Type or member is obsolete

            // Set the default stylesheet
            _userInterfaceManager.Stylesheet = SheetNanotrasen;

            sawmill.Debug($"Initialized {Stylesheets.Values.Sum(s => s.Rules.Count)} style rules in {sw.Elapsed}");
        }

        public bool TryGetStylesheet(string name, [MaybeNullWhen(false)] out Stylesheet stylesheet)
        {
            return Stylesheets.TryGetValue(name, out stylesheet);
        }
    }
}
