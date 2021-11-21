using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules;
using Content.Server.Hands.Components;
using Content.Server.Inventory.Components;
using Content.Server.Items;
using Content.Server.PDA;
using Content.Server.Players;
using Content.Server.Spawners.Components;
using Content.Server.Traitor;
using Content.Server.Traitor.Uplink;
using Content.Server.Traitor.Uplink.Account;
using Content.Server.Traitor.Uplink.Components;
using Content.Server.TraitorDeathMatch.Components;
using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Inventory;
using Content.Shared.MobState.Components;
using Content.Shared.Traitor.Uplink;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Presets
{
    [GamePreset("traitordm", "traitordeathmatch")]
    public sealed class PresetTraitorDeathMatch : GamePreset
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public string PDAPrototypeName => "CaptainPDA";
        public string BeltPrototypeName => "ClothingBeltJanitorFilled";
        public string BackpackPrototypeName => "ClothingBackpackFilled";

        private RuleMaxTimeRestart _restarter = default!;
        private bool _safeToEndRound = false;

        private Dictionary<UplinkAccount, string> _allOriginalNames = new();

        public override bool Start(IReadOnlyList<IPlayerSession> readyPlayers, bool force = false)
        {
            var gameTicker = EntitySystem.Get<GameTicker>();
            gameTicker.AddGameRule<RuleTraitorDeathMatch>();
            _restarter = gameTicker.AddGameRule<RuleMaxTimeRestart>();
            _restarter.RoundMaxTime = TimeSpan.FromMinutes(30);
            _restarter.RestartTimer();
            _safeToEndRound = true;
            return true;
        }

        public override void OnSpawnPlayerCompleted(IPlayerSession session, IEntity mob, bool lateJoin)
        {
            var startingBalance = _cfg.GetCVar(CCVars.TraitorDeathMatchStartingBalance);

            // Yup, they're a traitor
            var mind = session.Data.ContentData()?.Mind;
            if (mind == null)
            {
                Logger.ErrorS("preset", "Failed getting mind for TDM player.");
                return;
            }

            var traitorRole = new TraitorRole(mind);
            mind.AddRole(traitorRole);

            // Delete anything that may contain "dangerous" role-specific items.
            // (This includes the PDA, as everybody gets the captain PDA in this mode for true-all-access reasons.)
            if (mind.OwnedEntity != null && mind.OwnedEntity.TryGetComponent(out InventoryComponent? inventory))
            {
                var victimSlots = new[] {EquipmentSlotDefines.Slots.IDCARD, EquipmentSlotDefines.Slots.BELT, EquipmentSlotDefines.Slots.BACKPACK};
                foreach (var slot in victimSlots)
                {
                    if (inventory.TryGetSlotItem(slot, out ItemComponent? vItem))
                        vItem.Owner.Delete();
                }

                // Replace their items:

                //  pda
                var newPDA = _entityManager.SpawnEntity(PDAPrototypeName, mind.OwnedEntity.Transform.Coordinates);
                inventory.Equip(EquipmentSlotDefines.Slots.IDCARD, newPDA.GetComponent<ItemComponent>());

                //  belt
                var newTmp = _entityManager.SpawnEntity(BeltPrototypeName, mind.OwnedEntity.Transform.Coordinates);
                inventory.Equip(EquipmentSlotDefines.Slots.BELT, newTmp.GetComponent<ItemComponent>());

                //  backpack
                newTmp = _entityManager.SpawnEntity(BackpackPrototypeName, mind.OwnedEntity.Transform.Coordinates);
                inventory.Equip(EquipmentSlotDefines.Slots.BACKPACK, newTmp.GetComponent<ItemComponent>());

                // Like normal traitors, they need access to a traitor account.
                var uplinkAccount = new UplinkAccount(startingBalance, mind.OwnedEntity.Uid);
                var accounts = _entityManager.EntitySysManager.GetEntitySystem<UplinkAccountsSystem>();
                accounts.AddNewAccount(uplinkAccount);

                _entityManager.EntitySysManager.GetEntitySystem<UplinkSystem>()
                    .AddUplink(mind.OwnedEntity, uplinkAccount, newPDA);

                _allOriginalNames[uplinkAccount] = mind.OwnedEntity.Name;

                // The PDA needs to be marked with the correct owner.
                var pda = newPDA.GetComponent<PDAComponent>();
                _entityManager.EntitySysManager.GetEntitySystem<PDASystem>()
                    .SetOwner(pda, mind.OwnedEntity.Name);
                newPDA.AddComponent<TraitorDeathMatchReliableOwnerTagComponent>().UserId = mind.UserId;
            }

            // Finally, it would be preferrable if they spawned as far away from other players as reasonably possible.
            if (mind.OwnedEntity != null && FindAnyIsolatedSpawnLocation(mind, out var bestTarget))
            {
                mind.OwnedEntity.Transform.Coordinates = bestTarget;
            }
            else
            {
                // The station is too drained of air to safely continue.
                if (_safeToEndRound)
                {
                    _chatManager.DispatchServerAnnouncement(Loc.GetString("traitor-death-match-station-is-too-unsafe-announcement"));
                    _restarter.RoundMaxTime = TimeSpan.FromMinutes(1);
                    _restarter.RestartTimer();
                    _safeToEndRound = false;
                }
            }
        }

        // It would be nice if this function were moved to some generic helpers class.
        private bool FindAnyIsolatedSpawnLocation(Mind.Mind ignoreMe, out EntityCoordinates bestTarget)
        {
            // Collate people to avoid...
            var existingPlayerPoints = new List<EntityCoordinates>();
            foreach (var player in _playerManager.GetAllPlayers())
            {
                var avoidMeMind = player.Data.ContentData()?.Mind;
                if ((avoidMeMind == null) || (avoidMeMind == ignoreMe))
                    continue;
                var avoidMeEntity = avoidMeMind.OwnedEntity;
                if (avoidMeEntity == null)
                    continue;
                if (avoidMeEntity.TryGetComponent(out MobStateComponent? mobState))
                {
                    // Does have mob state component; if critical or dead, they don't really matter for spawn checks
                    if (mobState.IsCritical() || mobState.IsDead())
                        continue;
                }
                else
                {
                    // Doesn't have mob state component. Assume something interesting is going on and don't count this as someone to avoid.
                    continue;
                }
                existingPlayerPoints.Add(avoidMeEntity.Transform.Coordinates);
            }

            // Iterate over each possible spawn point, comparing to the existing player points.
            // On failure, the returned target is the location that we're already at.
            var bestTargetDistanceFromNearest = -1.0f;
            // Need the random shuffle or it stuffs the first person into Atmospherics pretty reliably
            var ents = _entityManager.EntityQuery<SpawnPointComponent>().Select(x => x.Owner).ToList();
            _robustRandom.Shuffle(ents);
            var foundATarget = false;
            bestTarget = EntityCoordinates.Invalid;
            var atmosphereSystem = EntitySystem.Get<AtmosphereSystem>();
            foreach (var entity in ents)
            {
                if (!atmosphereSystem.IsTileMixtureProbablySafe(entity.Transform.Coordinates))
                    continue;

                var distanceFromNearest = float.PositiveInfinity;
                foreach (var existing in existingPlayerPoints)
                {
                    if (entity.Transform.Coordinates.TryDistance(_entityManager, existing, out var dist))
                        distanceFromNearest = Math.Min(distanceFromNearest, dist);
                }
                if (bestTargetDistanceFromNearest < distanceFromNearest)
                {
                    bestTarget = entity.Transform.Coordinates;
                    bestTargetDistanceFromNearest = distanceFromNearest;
                    foundATarget = true;
                }
            }
            return foundATarget;
        }

        public override bool OnGhostAttempt(Mind.Mind mind, bool canReturnGlobal)
        {
            var entity = mind.OwnedEntity;
            if ((entity != null) && (entity.TryGetComponent(out MobStateComponent? mobState)))
            {
                if (mobState.IsCritical())
                {
                    // TODO BODY SYSTEM KILL
                    var damage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Asphyxiation"), 100);
                    EntitySystem.Get<DamageableSystem>().TryChangeDamage(entity.Uid, damage, true);
                }
                else if (!mobState.IsDead())
                {
                    if (entity.HasComponent<HandsComponent>())
                    {
                        return false;
                    }
                }
            }
            var session = mind.Session;
            if (session == null)
                return false;
            EntitySystem.Get<GameTicker>().Respawn(session);
            return true;
        }

        public override string GetRoundEndDescription()
        {
            var lines = new List<string>();
            lines.Add(Loc.GetString("traitor-death-match-end-round-description-first-line"));
            foreach (var uplink in _entityManager.EntityQuery<UplinkComponent>(true))
            {
                var uplinkAcc = uplink.UplinkAccount;
                if (uplinkAcc != null && _allOriginalNames.ContainsKey(uplinkAcc))
                {
                    lines.Add(Loc.GetString("traitor-death-match-end-round-description-entry",
                                            ("originalName", _allOriginalNames[uplinkAcc]),
                                            ("tcBalance", uplinkAcc.Balance)));
                }
            }
            return string.Join('\n', lines);
        }

        public override string ModeTitle => Loc.GetString("traitor-death-match-title");
        public override string Description => Loc.GetString("traitor-death-match-description");
    }
}
