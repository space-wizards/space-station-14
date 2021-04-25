#nullable enable
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.Actions;
using Content.Shared.Alert;
using System;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

namespace Content.Server.GameObjects.Components
{ 
    public sealed class SpellBookComponent : IUse
    {
        [ViewVariables] [DataField("spells")] public List<ActionType>? GrantedSpells { get; set; }
        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent<SharedActionsComponent>(out var actions)) return false;
            foreach(var spell in GrantedSpells)
            {
                actions.Grant(spell);
            }
        }
    }
}
