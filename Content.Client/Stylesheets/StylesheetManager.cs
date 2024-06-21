using Content.Client.Stylesheets.Redux;
using Content.Client.Stylesheets.Redux.Stylesheets;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.IoC;

namespace Content.Client.Stylesheets
{
    public sealed class StylesheetManager : IStylesheetManager
    {
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;

        public Stylesheet SheetNano { get; private set; } = default!;
        public Stylesheet SheetInterface { get; private set; } = default!;

        public Stylesheet SheetSpace { get; private set; } = default!;

        public void Initialize()
        {
            SheetNano = new NanotrasenStylesheet(new PalettedStylesheet.NoConfig()).Stylesheet;
            SheetInterface = new InterfaceStylesheet(new PalettedStylesheet.NoConfig()).Stylesheet;
            SheetSpace = new StyleSpace(_resourceCache).Stylesheet;

            _userInterfaceManager.Stylesheet = SheetNano;
        }
    }
}
