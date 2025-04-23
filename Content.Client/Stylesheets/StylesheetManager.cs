using Content.Client._Starlight;
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
        public Stylesheet SheetSpace { get; private set; } = default!;
        public Stylesheet Starlight { get; private set; } = default!;  //🌟Starlight🌟

        public void Initialize()
        {
            SheetNano = new StyleNano(_resourceCache).Stylesheet;
            SheetSpace = new StyleSpace(_resourceCache).Stylesheet;
            Starlight = new StyleStarlight(_resourceCache).Stylesheet; //🌟Starlight🌟
            _userInterfaceManager.Stylesheet = SheetNano;
        }
    }
}
