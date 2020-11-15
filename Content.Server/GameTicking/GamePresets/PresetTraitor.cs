using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.PDA;
using Content.Server.GameTicking.GameRules;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Mobs.Roles.Traitor;
using Content.Server.Players;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.GameObjects.Components.PDA;
using Content.Shared.Roles;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.GamePresets
{
    public class PresetTraitor : GamePreset
    {
        [Dependency] private IGameTicker _gameticker = default!;
        [Dependency] private IChatManager _chatManager = default!;
        [Dependency] private IPrototypeManager _prototypeManager = default!;
        [Dependency] private IRobustRandom _random = default!;

        public override string ModeTitle => "Traitor";

        //make these cvars
        private int MinPlayers => 2;
        private int TraitorPerPlayers => 5;
        private int MaxTraitors => 4;
        private int CodewordCount => 2;
        private int StartingTC => 20;

        private string[] Codewords => new[] {"cold", "winter", "radiator", "average", "furious"};

        public override bool Start(IReadOnlyList<IPlayerSession> readyPlayers, bool force = false)
        {
            if (!force && readyPlayers.Count < MinPlayers)
            {
                _chatManager.DispatchServerAnnouncement($"Not enough players readied up for the game! There were {readyPlayers.Count} players readied up out of {MinPlayers} needed.");
                return false;
            }

            if (readyPlayers.Count == 0)
            {
                _chatManager.DispatchServerAnnouncement("No players readied up! Can't start Suspicion.");
                return false;
            }

            var list = new List<IPlayerSession>(readyPlayers);
            var prefList = new List<IPlayerSession>();

            foreach (var player in list)
            {
                if (!readyProfiles.ContainsKey(player.UserId))
                {
                    continue;
                }
                var profile = readyProfiles[player.UserId];
                if (profile.AntagPreferences.Contains("Traitor"))
                {
                    prefList.Add(player);
                }
            }

            var numTraitors = MathHelper.Clamp(readyPlayers.Count / TraitorPerPlayers,
                1, MaxTraitors);

            var traitors = new List<TraitorRole>();

            for (var i = 0; i < numTraitors; i++)
            {
                IPlayerSession traitor;
                if(prefList.Count == 0)
                {
                    if (list.Count == 0)
                    {
                        Logger.InfoS("preset", "Insufficient ready players to fill up with traitors, stopping the selection.");
                        break;
                    }
                    traitor = _random.PickAndTake(list);
                    Logger.InfoS("preset", "Insufficient preferred traitors, picking at random.");
                }
                else
                {
                    traitor = _random.PickAndTake(prefList);
                    list.Remove(traitor);
                    Logger.InfoS("preset", "Selected a preferred traitor.");
                }
                var mind = traitor.Data.ContentData()?.Mind;
                var traitorRole = new TraitorRole(mind);
                if (mind == null)
                {
                    Logger.ErrorS("preset", "Failed getting mind for picked traitor.");
                    continue;
                }

                // creadth: we need to create uplink for the antag.
                // PDA should be in place already, so we just need to
                // initiate uplink account.
                var uplinkAccount = new UplinkAccount(mind.OwnedEntity.Uid, StartingTC);
                var inventory = mind.OwnedEntity.GetComponent<InventoryComponent>();
                if (!inventory.TryGetSlotItem(EquipmentSlotDefines.Slots.IDCARD, out ItemComponent pdaItem))
                {
                    Logger.ErrorS("preset", "Failed getting pda for picked traitor.");
                    continue;
                }

                var pda = pdaItem.Owner;

                var pdaComponent = pda.GetComponent<PDAComponent>();
                if (pdaComponent.IdSlotEmpty)
                {
                    Logger.ErrorS("preset","PDA had no id for picked traitor");
                    continue;
                }

                mind.AddRole(traitorRole);
                traitors.Add(traitorRole);
                pdaComponent.InitUplinkAccount(uplinkAccount);
            }

            //todo give traitors their objectives

            var codewordPool = new List<string>(Codewords);
            var finalCodewordCount = Math.Min(CodewordCount, Codewords.Length);
            string[] codewords = new string[finalCodewordCount];
            for (int i = 0; i < finalCodewordCount; i++)
            {
                codewords[i] = _random.PickAndTake(codewordPool);
            }

            foreach (var traitor in traitors)
            {
                traitor.GreetTraitor(codewords);
            }

            _gameticker.AddGameRule<RuleTraitor>();
            return true;
        }
    }
}
