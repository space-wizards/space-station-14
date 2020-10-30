using Content.Client.Administration;
using Content.Client.Chat;
using Content.Client.GameTicking;
using Content.Client.Interfaces;
using Content.Client.Interfaces.Chat;
using Content.Client.Interfaces.Parallax;
using Content.Client.Parallax;
using Content.Client.Sandbox;
using Content.Client.StationEvents;
using Content.Client.UserInterface;
using Content.Client.UserInterface.AdminMenu;
using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Content.Shared.Interfaces;
using Robust.Shared.IoC;

namespace Content.Client
{
    internal static class ClientContentIoC
    {
        public static void Register()
        {
            IoCManager.Register<IGameHud, GameHud>();
            IoCManager.Register<IClientNotifyManager, ClientNotifyManager>();
            IoCManager.Register<ISharedNotifyManager, ClientNotifyManager>();
            IoCManager.Register<IClientGameTicker, ClientGameTicker>();
            IoCManager.Register<IParallaxManager, ParallaxManager>();
            IoCManager.Register<IChatManager, ChatManager>();
            IoCManager.Register<IEscapeMenuOwner, EscapeMenuOwner>();
            IoCManager.Register<ISandboxManager, SandboxManager>();
            IoCManager.Register<IModuleManager, ClientModuleManager>();
            IoCManager.Register<IClientPreferencesManager, ClientPreferencesManager>();
            IoCManager.Register<IItemSlotManager, ItemSlotManager>();
            IoCManager.Register<IStylesheetManager, StylesheetManager>();
            IoCManager.Register<IScreenshotHook, ScreenshotHook>();
            IoCManager.Register<IClickMapManager, ClickMapManager>();
            IoCManager.Register<IStationEventManager, StationEventManager>();
            IoCManager.Register<IAdminMenuManager, AdminMenuManager>();
            IoCManager.Register<IClientAdminManager, ClientAdminManager>();
        }
    }
}
