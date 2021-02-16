#nullable enable
using System;
using System.Collections.Generic;
using Robust.Server.Player;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Random;

namespace Content.Server.Voting
{
    public sealed partial class VoteManager
    {
        public void CreateRestartVote(IPlayerSession? initiator)
        {
            var alone = _playerManager.PlayerCount == 1 && initiator != null;
            var options = new VoteOptions
            {
                Title = Loc.GetString("Restart round"),
                Options =
                {
                    (Loc.GetString("Yes"), true),
                    (Loc.GetString("No"), false)
                },
                Duration = alone
                    ? TimeSpan.FromSeconds(10)
                    : TimeSpan.FromSeconds(30)
            };

            if (alone)
                options.InitiatorTimeout = TimeSpan.FromSeconds(10);

            WirePresetVoteInitiator(options, initiator);

            var vote = CreateVote(options);

            vote.OnFinished += (_, args) =>
            {
                if (args.Winner == null)
                {
                    _chatManager.DispatchServerAnnouncement(Loc.GetString("Restart vote failed due to tie."));
                    return;
                }

                var win = (bool) args.Winner;
                if (win)
                {
                    _chatManager.DispatchServerAnnouncement(Loc.GetString("Restart vote succeeded."));
                    _ticker.RestartRound();
                }
                else
                {
                    _chatManager.DispatchServerAnnouncement(Loc.GetString("Restart vote failed."));
                }
            };

            if (initiator != null)
            {
                // Cast yes vote if created the vote yourself.
                vote.CastVote(initiator, 0);
            }
        }

        public void CreatePresetVote(IPlayerSession? initiator)
        {
            var presets = new Dictionary<string, string>
            {
                ["traitor"] = "Traitor",
                ["extended"] = "Extended",
                ["sandbox"] = "Sandbox",
                ["suspicion"] = "Suspicion"
            };

            var alone = _playerManager.PlayerCount == 1 && initiator != null;
            var options = new VoteOptions
            {
                Title = Loc.GetString("Next gamemode"),
                Duration = alone
                    ? TimeSpan.FromSeconds(10)
                    : TimeSpan.FromSeconds(30)
            };

            if (alone)
                options.InitiatorTimeout = TimeSpan.FromSeconds(10);

            foreach (var (k, v) in presets)
            {
                options.Options.Add((Loc.GetString(v), k));
            }

            WirePresetVoteInitiator(options, initiator);

            var vote = CreateVote(options);

            vote.OnFinished += (_, args) =>
            {
                string picked;
                if (args.Winner == null)
                {
                    picked = (string) IoCManager.Resolve<IRobustRandom>().Pick(args.Winners);
                    _chatManager.DispatchServerAnnouncement(
                        Loc.GetString("Tie for gamemode vote! Picking... {0}", Loc.GetString(presets[picked])));
                }
                else
                {
                    picked = (string) args.Winner;
                    _chatManager.DispatchServerAnnouncement(
                        Loc.GetString("{0} won the gamemode vote!", Loc.GetString(presets[picked])));
                }

                _ticker.SetStartPreset(picked);
            };
        }
    }
}
