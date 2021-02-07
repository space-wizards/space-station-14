using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.UserInterface;
using Robust.Shared.IoC;

namespace Content.Client.UserInterface.Stylesheets
{
    public sealed class StylesheetManager : IStylesheetManager
    {
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;

        public Stylesheet SheetNano { get; private set; }
        public Stylesheet SheetSpace { get; private set; }

        public void Initialize()
        {
            SheetNano = new StyleNano(_resourceCache).Stylesheet;
            SheetSpace = new StyleSpace(_resourceCache).Stylesheet;

            _userInterfaceManager.Stylesheet = SheetNano;
        }
    }
}
