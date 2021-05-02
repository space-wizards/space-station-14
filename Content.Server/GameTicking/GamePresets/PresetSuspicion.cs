using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.PDA;
using Content.Server.GameObjects.Components.Suspicion;
using Content.Server.GameTicking.GameRules;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Mobs.Roles.Suspicion;
using Content.Server.Players;
using Content.Shared;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.GameObjects.Components.PDA;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.GamePresets
{
    [GamePreset("suspicion")]
    public class PresetSuspicion : GamePreset
    {
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IGameTicker _gameTicker = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public int MinPlayers { get; set; }
        public int MinTraitors { get; set; }
        public int PlayersPerTraitor { get; set; }

        public int TraitorStartingBalance { get; set; }


        public override bool DisallowLateJoin => true;

        private static string TraitorID = "SuspicionTraitor";
        private static string InnocentID = "SuspicionInnocent";

        public override bool Start(IReadOnlyList<IPlayerSession> readyPlayers, bool force = false)
        {
            MinPlayers = _cfg.GetCVar(CCVars.SuspicionMinPlayers);
            MinTraitors = _cfg.GetCVar(CCVars.SuspicionMinTraitors);
            PlayersPerTraitor = _cfg.GetCVar(CCVars.SuspicionPlayersPerTraitor);
            TraitorStartingBalance = _cfg.GetCVar(CCVars.SuspicionStartingBalance);

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
                if (!ReadyProfiles.ContainsKey(player.UserId))
                {
                    continue;
                }
                prefList.Add(player);

                player.AttachedEntity?.EnsureComponent<SuspicionRoleComponent>();
            }

            var numTraitors = MathHelper.Clamp(readyPlayers.Count / PlayersPerTraitor,
                MinTraitors, readyPlayers.Count);

            var traitors = new List<SuspicionTraitorRole>();

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
                var antagPrototype = _prototypeManager.Index<AntagPrototype>(TraitorID);

                DebugTools.AssertNotNull(mind?.OwnedEntity);

                var traitorRole = new SuspicionTraitorRole(mind!, antagPrototype);
                mind!.AddRole(traitorRole);
                traitors.Add(traitorRole);
                // creadth: we need to create uplink for the antag.
                // PDA should be in place already, so we just need to
                // initiate uplink account.
                var uplinkAccount =
                    new UplinkAccount(mind.OwnedEntity!.Uid,
                        TraitorStartingBalance);
                var inventory = mind.OwnedEntity.GetComponent<InventoryComponent>();
                if (!inventory.TryGetSlotItem(EquipmentSlotDefines.Slots.IDCARD, out ItemComponent? pdaItem))
                {
                    continue;
                }

                var pda = pdaItem.Owner;

                var pdaComponent = pda.GetComponent<PDAComponent>();
                if (pdaComponent.IdSlotEmpty)
                {
                    continue;
                }

                pdaComponent.InitUplinkAccount(uplinkAccount);

            }

            foreach (var player in list)
            {
                var mind = player.Data.ContentData()?.Mind;
                var antagPrototype = _prototypeManager.Index<AntagPrototype>(InnocentID);

                DebugTools.AssertNotNull(mind);

                mind!.AddRole(new SuspicionInnocentRole(mind, antagPrototype));
            }

            foreach (var traitor in traitors)
            {
                traitor.GreetSuspicion(traitors, _chatManager);
            }

            _gameTicker.AddGameRule<RuleSuspicion>();
            return true;
        }

        public override string ModeTitle => "Suspicion";
        public override string Description => "Suspicion on the Space Station. There are traitors on board... Can you kill them before they kill you?";
    }
}
