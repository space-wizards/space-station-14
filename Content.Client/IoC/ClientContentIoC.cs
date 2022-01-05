using Content.Client.Administration;
using Content.Client.Administration.Managers;
using Content.Client.Changelog;
using Content.Client.Chat.Managers;
using Content.Client.Clickable;
using Content.Client.EscapeMenu;
using Content.Client.Eui;
using Content.Client.HUD;
using Content.Client.Info;
using Content.Client.Items.Managers;
using Content.Client.Module;
using Content.Client.Parallax.Managers;
using Content.Client.Preferences;
using Content.Client.Sandbox;
using Content.Client.Screenshot;
using Content.Client.StationEvents.Managers;
using Content.Client.Stylesheets;
using Content.Client.Viewport;
using Content.Client.Voting;
using Content.Shared.Actions;
using Content.Shared.Administration;
using Content.Shared.Module;
using Robust.Shared.IoC;

namespace Content.Client.IoC
{
    internal static class ClientContentIoC
    {
        public static void Register()
        {
            IoCManager.Register<IGameHud, GameHud>();
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
            IoCManager.Register<ActionManager, ActionManager>();
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
