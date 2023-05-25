using System.Linq;
using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared.Ghost.Roles;
using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Client.UserInterface.Systems.Ghost.Controls.Roles
{
    [UsedImplicitly]
    public sealed class GhostRolesEui : BaseEui
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        private readonly GhostRolesWindow _window;
        private GhostRoleRulesWindow? _windowRules = null;

        private uint _windowRulesId = 0;
        private string _windowRulesName = "";

        public GhostRolesEui()
        {
            _window = new GhostRolesWindow();

            _window.OnRoleRequested += info =>
            {
                if (_windowRules != null)
                    _windowRules.Close();

                if (info.Rules != _window.LastRulesAccepted)
                {
                    // First ghost role you accept as a specific ghost, you get shown the rules
                    //   OR if the rules are different from the last ghost role you attempted to get.
                    _windowRules = new GhostRoleRulesWindow(info.Rules, _ =>
                    {
                        _window.LastRulesAccepted = info.Rules;
                        // If the user was not too slow, attempt to take it.
                        if (!_windowRules?.RoleHasGone ?? true)
                        {
                            // Note, _windowRulesId might have changed while dialog was shown if the previous role
                            //   (with same name) was no longer available (drat faster players!)
                            SendMessage(new GhostRoleTakeoverRequestMessage(_windowRulesId));
                        }

                        _windowRules?.Close();
                    });

                    _windowRulesId = info.Identifier;
                    _windowRulesName = info.Name;
                    _windowRules.OnClose += () =>
                    {
                        _windowRules = null;
                    };
                    _windowRules.OpenCentered();
                }
                else
                {
                    // If you accepted a role which went away (likely on a multiplayer server) you don't need to read
                    //   the SAME rules again for 3 seconds prior to accepting the role.
                    SendMessage(new GhostRoleTakeoverRequestMessage(info.Identifier));
                }
            };

            _window.OnRoleFollow += info =>
            {
                SendMessage(new GhostRoleFollowRequestMessage(info.Identifier));
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
            _window.LastRulesAccepted = null;
            _window.Close();
            _windowRules?.Close();
        }

        public override void HandleState(EuiStateBase state)
        {
            base.HandleState(state);

            if (state is not GhostRolesEuiState ghostState) return;
            _window.ClearEntries();

            var groupedRoles = ghostState.GhostRoles.GroupBy(
                role => (role.Name, role.Description));
            foreach (var group in groupedRoles)
            {
                var name = group.Key.Name;
                var description = group.Key.Description;

                _window.AddEntry(name, description, group);
            }

            if (_windowRules != null && !_windowRules.RoleHasGone)
            {
                // Perhaps the user won't be able to get this role now.
                _windowRules.RoleHasGone = ghostState.GhostRoles.All(role => role.Identifier != _windowRulesId);

                if (_windowRules.RoleHasGone)
                {
                    var replacements =
                        ghostState.GhostRoles.Where(role => role.Name == _windowRulesName).Select(role=> role.Identifier).ToList();

                    if (replacements.Count == 1)
                    {
                        _windowRulesId = replacements[0];
                        _windowRules.RoleHasGone = false;
                    }
                    else if (replacements.Count >= 2)
                    {
                        // Pick randomly or else multiple queued players will all drop until the first element
                        var index = _random.Next(replacements.Count - 1);
                        _windowRulesId = replacements[index];
                        _windowRules.RoleHasGone = false;
                    }
                }
            }
        }
    }
}
