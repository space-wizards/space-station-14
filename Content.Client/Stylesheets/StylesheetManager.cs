using Content.Client.Stylesheets.Redux;
using Content.Client.Stylesheets.Redux.Stylesheets;
using Content.Client.UserInterface.Screens;
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
            var nanoBase = new NanotrasenStylesheet(new PalettedStylesheet.NoConfig());
            _defaultSheetBase = nanoBase;
            SheetNano = Init(nanoBase);
            SheetInterface = new InterfaceStylesheet(new PalettedStylesheet.NoConfig()).Stylesheet;
            SheetSpace = new StyleSpace(_resourceCache).Stylesheet;

            _userInterfaceManager.Stylesheet = SheetNano;
            // _userInterfaceManager.OnScreenChanged += OnScreenChanged;
        }

        private Dictionary<Stylesheet, BaseStylesheet> _sheetToBaseSheet = new();
        private BaseStylesheet _defaultSheetBase = default!;

        public Stylesheet Init(BaseStylesheet baseSheet)
        {
            _sheetToBaseSheet.Add(baseSheet.Stylesheet, baseSheet);
            return baseSheet.Stylesheet;
        }

        public BaseStylesheet GetBaseStylesheet(Stylesheet? sheet)
        {
            if (sheet is null)
                return _defaultSheetBase;
            try
            {
                return _sheetToBaseSheet[sheet];
            }
            catch
            {
                return _defaultSheetBase;
            }
        }

        // NOTE: taken out b/c chat colors being different looked slightly wacky

        // // Required because stylesheet is initialized before .ActiveScreen is set on UiManager and after the
        // // HUD UIs are actually constructed.
        // private void OnScreenChanged((UIScreen? Old, UIScreen? New) ev)
        // {
        //     if (ev.New is not null)
        //         ev.New.Stylesheet = SheetInterface;
        // }
    }
}
