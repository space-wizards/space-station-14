using Content.Client.Administration.Managers;
using Content.Client.Changelog;
using Content.Client.Chat.Managers;
using Content.Client.Clickable;
using Content.Client.Options;
using Content.Client.Eui;
using Content.Client.Gameplay;
using Content.Client.HUD;
using Content.Client.Info;
using Content.Client.Module;
using Content.Client.Parallax.Managers;
using Content.Client.Preferences;
using Content.Client.Screenshot;
using Content.Client.StationEvents.Managers;
using Content.Client.Stylesheets;
using Content.Client.UserInterface;
using Content.Client.Viewport;
using Content.Client.Voting;
using Content.Shared.Actions;
using Content.Shared.Administration;
using Content.Shared.Module;

namespace Content.Client.IoC
{
    internal static class ClientContentIoC
    {
        public static void Register()
        {
            IoCManager.Register<IHudManager, HudManager>();
            IoCManager.Register<IParallaxManager, ParallaxManager>();
            IoCManager.Register<IChatManager, ChatManager>();
            IoCManager.Register<IModuleManager, ClientModuleManager>();
            IoCManager.Register<IClientPreferencesManager, ClientPreferencesManager>();
            IoCManager.Register<IStylesheetManager, StylesheetManager>();
            IoCManager.Register<IScreenshotHook, ScreenshotHook>();
            IoCManager.Register<IClickMapManager, ClickMapManager>();
            IoCManager.Register<IStationEventManager, StationEventManager>();
            IoCManager.Register<IClientAdminManager, ClientAdminManager>();
            IoCManager.Register<EuiManager, EuiManager>();
            IoCManager.Register<IVoteManager, VoteManager>();
            IoCManager.Register<ChangelogManager, ChangelogManager>();
            IoCManager.Register<RulesManager, RulesManager>();
            IoCManager.Register<ViewportManager, ViewportManager>();
            IoCManager.Register<IGamePrototypeLoadManager, GamePrototypeLoadManager>();
        }
    }
}
