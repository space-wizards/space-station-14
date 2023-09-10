// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Content.Shared.Popups;
using Robust.Shared.Console;
using Robust.Shared.Utility;
using Robust.Shared.Players;
using Content.Shared.SS220.GhostRoleCast;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.GhostRoleCast
{
    public sealed class GhostRoleCastSystem : EntitySystem
    {
        [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly IConsoleHost _consoleHost = default!;
        [Dependency] private readonly IUserInterfaceManager _uimanager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GhostRoleCastComponent, ComponentStartup>(OnGhostRoleCastInit);

            SubscribeLocalEvent<GhostRoleCastComponent, ToggleGhostRoleCastActionEvent>(OnToggleGhostRoleCast);
            SubscribeLocalEvent<GhostRoleCastComponent, ToggleGhostRoleRemoveActionEvent>(OnToggleGhostRoleRemove);
            SubscribeLocalEvent<GhostRoleCastComponent, ToggleGhostRoleCastSettingsEvent>(OnToggleGhostRoleSettings);
        }

        private void OnGhostRoleCastInit(EntityUid uid, GhostRoleCastComponent component, ComponentStartup args)
        {
            component.ToggleGhostRoleNameAction = Spawn(GhostRoleCastComponent.ToggleGhostRoleNameActionId);
            component.ToggleGhostRoleCastAction = Spawn(GhostRoleCastComponent.ToggleGhostRoleCastActionId);
            component.ToggleGhostRoleRemoveAction = Spawn(GhostRoleCastComponent.ToggleGhostRoleRemoveActionId);

            _actions.AddAction(uid, ref component.ToggleGhostRoleNameAction, null);
            _actions.AddAction(uid, ref component.ToggleGhostRoleCastAction, null);
            _actions.AddAction(uid, ref component.ToggleGhostRoleRemoveAction, null);
        }

        private void OnToggleGhostRoleCast(EntityUid uid, GhostRoleCastComponent component, ToggleGhostRoleCastActionEvent args)
        {
            if (args.Handled)
                return;

            _popup.PopupEntity(Loc.GetString("action-toggle-ghostrole-cast-popup"), args.Performer);

            var flag = _playerManager.TryGetSessionByEntity(args.Performer, out var playersession);
            if (flag == false)
                return;

            var rolename = component.GhostRoleName;
            var roledesc = component.GhostRoleDesc;
            var rolerule = component.GhostRoleRule;

            if (rolename == "")
                rolename = EntityManager.GetComponent<MetaDataComponent>(args.Target).EntityName;
            if (roledesc == "")
                roledesc = EntityManager.GetComponent<MetaDataComponent>(args.Target).EntityName;
            if (rolerule == "")
                rolerule = Loc.GetString("ghost-role-component-default-rules");

            var makeGhostRoleCommand =
                $"makeghostrole " +
                $"\"{CommandParsing.Escape(args.Target.ToString())}\" " +
                $"\"{CommandParsing.Escape(rolename)}\" " +
                $"\"{CommandParsing.Escape(roledesc)}\" " +
                $"\"{CommandParsing.Escape(rolerule)}\"";

            _consoleHost.ExecuteCommand(playersession, makeGhostRoleCommand);

            //if (makesentient)
            //{
            //    var makesentientcommand = $"makesentient \"{commandparsing.escape(uid.tostring())}\"";
            //    _consolehost.executecommand(player.session, makesentientcommand);
            //}

            args.Handled = true;
        }

        private void OnToggleGhostRoleRemove(EntityUid uid, GhostRoleCastComponent component, ToggleGhostRoleRemoveActionEvent args)
        {
            if (args.Handled)
                return;

            _popup.PopupEntity(Loc.GetString("action-toggle-ghostrole-remove-popup"), args.Performer);

            var flag = _playerManager.TryGetSessionByEntity(args.Performer, out var playersession);
            if (flag == false)
            {
                return;
            }

            var removeGhostRoleCommand =
                $"rmcomp " +
                $"\"{CommandParsing.Escape(args.Target.ToString())}\" " +
                $"\"{CommandParsing.Escape("GhostRole")}\"";

            _consoleHost.ExecuteCommand(playersession, removeGhostRoleCommand);

            var removeGhostTakeoverAvailableCommand =
                $"rmcomp " +
                $"\"{CommandParsing.Escape(args.Target.ToString())}\" " +
                $"\"{CommandParsing.Escape("GhostTakeoverAvailable")}\"";

            _consoleHost.ExecuteCommand(playersession, removeGhostTakeoverAvailableCommand);

            args.Handled = true;
        }

        private void OnToggleGhostRoleSettings(EntityUid uid, GhostRoleCastComponent component, ToggleGhostRoleCastSettingsEvent args)
        {
            if (args.Handled)
                return;

            var uicontroller = _uimanager.GetUIController<GhostRoleCastUIController>();
            uicontroller.ToggleWindow();
        }
    }
}
