using System.Linq;
using Content.Client.Eui;
using Content.Client.Players.PlayTimeTracking;
using Content.Shared.Eui;
using Content.Shared.Ghost.Roles;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Ghost.Controls.Roles
{
    [UsedImplicitly]
    public sealed class GhostRolesEui : BaseEui
    {
        private readonly GhostRolesWindow _window;
        private GhostRoleRulesWindow? _windowRules = null;
        private uint _windowRulesId = 0;

        public GhostRolesEui()
        {
            _window = new GhostRolesWindow();

            _window.OnRoleRequestButtonClicked += info =>
            {
                _windowRules?.Close();

                if (info.Kind == GhostRoleKind.RaffleJoined)
                {
                    SendMessage(new LeaveGhostRoleRaffleMessage(info.Identifier));
                    return;
                }

                _windowRules = new GhostRoleRulesWindow(info.Rules, _ =>
                {
                    SendMessage(new RequestGhostRoleMessage(info.Identifier));

                    // if raffle role, close rules window on request, otherwise do
                    // old behavior of waiting for the server to close it
                    if (info.Kind != GhostRoleKind.FirstComeFirstServe)
                        _windowRules?.Close();
                });
                _windowRulesId = info.Identifier;
                _windowRules.OnClose += () =>
                {
                    _windowRules = null;
                };
                _windowRules.OpenCentered();
            };

            _window.OnRoleFollow += info =>
            {
                SendMessage(new FollowGhostRoleMessage(info.Identifier));
            };

            _window.OnClose += () =>
            {
                SendMessage(new CloseEuiMessage());
            };
        }

        public override void Opened()
        {
            base.Opened();
            _window.OpenCentered();
        }

        public override void Closed()
        {
            base.Closed();
            _window.Close();
            _windowRules?.Close();
        }

        public override void HandleState(EuiStateBase state)
        {
            base.HandleState(state);

            if (state is not GhostRolesEuiState ghostState)
                return;
            _window.ClearEntries();

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var sysManager = entityManager.EntitySysManager;
            var spriteSystem = sysManager.GetEntitySystem<SpriteSystem>();
            var requirementsManager = IoCManager.Resolve<JobRequirementsManager>();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            var groupedRoles = ghostState.GhostRoles.GroupBy(
                role => (role.Name, role.Description, role.Requirements, role.JobProto));
            foreach (var group in groupedRoles)
            {
                var name = group.Key.Name;
                var description = group.Key.Description;
                var jobProto = group.Key.JobProto;
                bool hasAccess = true;
                FormattedMessage? reason;

                if (jobProto != null)
                {
                    var job = prototypeManager.Index<JobPrototype>(jobProto.Value.Id);
                    if (!requirementsManager.IsAllowed(job, null, out reason))
                        hasAccess = false;
                }
                else
                {
                    if (!requirementsManager.CheckRoleRequirements(group.Key.Requirements, null, out reason))
                        hasAccess = false;
                }

                _window.AddEntry(name, description, hasAccess, reason, group, spriteSystem);
            }

            var closeRulesWindow = ghostState.GhostRoles.All(role => role.Identifier != _windowRulesId);
            if (closeRulesWindow)
            {
                _windowRules?.Close();
            }
        }
    }
}
