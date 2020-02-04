using System;
using System.Collections.Generic;
using Content.Shared.Preferences;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Random;

namespace Content.Server.GameTicking
{
    public partial class GameTicker
    {
        private Dictionary<string, int> _presetRoles = new Dictionary<string, int>();

        public void AddPresetRole(string presetId, int percent)
        {
            _presetRoles.Add(presetId, percent);
        }

        private Dictionary<IPlayerSession, List<string>> AssignAntagRoles(List<IPlayerSession> available,
            Dictionary<IPlayerSession, HumanoidCharacterProfile> profiles)
        {
            var gameModeRoles = new Dictionary<string, int>();
            foreach (var role in _presetRoles)
            {
                gameModeRoles.Add(role.Key, Math.Max((int)(available.Count * (role.Value / 100f)), 1)); // 25% of players get traitor. Minimum is 1.
            };

            var assigned = new Dictionary<IPlayerSession, List<string>>();

            var antagRoles = new Dictionary<string, List<IPlayerSession>>();

            // TODO Replace with PlayerPrefs from profiles
            var AntagPrefs = new List<string>
            {
                "Traitor"
            };

            foreach (var player in available)
            {
                foreach (var role in AntagPrefs) // TODO profiles[player].AntagPrefs)
                {
                    if (!antagRoles.ContainsKey(role))
                        antagRoles.Add(role, new List<IPlayerSession> {
                            player
                        });
                    else
                        antagRoles[role].Add(player);
                }
            }

            foreach (var (role, players) in antagRoles)
            {
                if (!gameModeRoles.ContainsKey(role))
                    continue;

                while (gameModeRoles[role] > 0)
                {
                    var picked = _robustRandom.Pick(players);
                    if (!assigned.ContainsKey(picked))
                        assigned.Add(picked, new List<string>
                        {
                            role
                        });
                    else
                        assigned[picked].Add(role);
                    gameModeRoles[role] -= 1;
                }
            }
            return assigned;
        }
    }
}
