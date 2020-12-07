using System.Collections.Generic;
using Content.Server.GameTicking.GameRules;
using Content.Server.Interfaces.GameTicking;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.PDA;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Mobs;
using Content.Server.Mobs.Roles.Traitor;
using Content.Server.Players;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.GameObjects.Components.PDA;
using Content.Shared.GameObjects.Components.Mobs.State;
using Content.Shared;
using Robust.Server.Interfaces.Player;
using Robust.Server.Interfaces.Console;
﻿using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Log;

namespace Content.Server.GameTicking.GamePresets
{
    public sealed class PresetTraitorDeathMatch : GamePreset
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IGameTicker _gameTicker = default!;

        public string PDAPrototypeName => "CaptainPDA";
        public string BeltPrototypeName => "ClothingBeltJanitorFilled";
        public string BackpackPrototypeName => "ClothingBackpackFilled";

        public override bool Start(IReadOnlyList<IPlayerSession> readyPlayers, bool force = false)
        {
            // For now, don't setup anything for scoring/termination.
            return true;
        }

        public override void OnSpawnPlayerCompleted(IPlayerSession session, IEntity mob, bool lateJoin)
        {
            int startingBalance = _cfg.GetCVar(CCVars.TraitorDeathMatchStartingBalance);

            // Yup, they're a traitor
            var mind = session.Data.ContentData()?.Mind;
            var traitorRole = new TraitorRole(mind);
            if (mind == null)
            {
                Logger.ErrorS("preset", "Failed getting mind for TDM player.");
                return;
            }

            mind.AddRole(traitorRole);

            // Delete anything that may contain "dangerous" role-specific items.
            // (This includes the PDA, as everybody gets the captain PDA in this mode for true-all-access reasons.)
            var inventory = mind.OwnedEntity.GetComponent<InventoryComponent>();
            EquipmentSlotDefines.Slots[] victimSlots = new EquipmentSlotDefines.Slots[] {EquipmentSlotDefines.Slots.IDCARD, EquipmentSlotDefines.Slots.BELT, EquipmentSlotDefines.Slots.BACKPACK};
            foreach (var slot in victimSlots)
                if (inventory.TryGetSlotItem(slot, out ItemComponent vItem))
                    vItem.Owner.Delete();

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
            var uplinkAccount = new UplinkAccount(mind.OwnedEntity.Uid, startingBalance);
            newPDA.GetComponent<PDAComponent>().InitUplinkAccount(uplinkAccount);
        }

        public override bool OnGhostAttempt(Mind mind, bool canReturnGlobal)
        {
            var entity = mind.OwnedEntity;
            if ((entity != null) && (entity.TryGetComponent(out IMobStateComponent mobState)))
            {
                if (mobState.IsCritical())
                {
                    // TODO: This is copy/pasted from ghost code. Really, IDamagableComponent needs a method to reliably kill the target.
                    if (entity.TryGetComponent(out IDamageableComponent damageable))
                    {
                        //todo: what if they dont breathe lol
                        damageable.ChangeDamage(DamageType.Asphyxiation, 100, true);
                    }
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
            _gameTicker.Respawn(session);
            return true;
        }

        public override string ModeTitle => "Traitor Deathmatch";
        public override string Description => Loc.GetString("Everyone's a traitor. Everyone wants each other dead.");
    }
}
