#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Power;
using Content.Shared.Audio;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Weapon.Melee
{
    [RegisterComponent]
    public class StunbatonComponent : Component, IInteractUsing
    {
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        public override string Name => "Stunbaton";

        public bool Activated = false;

        [ViewVariables]
        [ComponentDependency]
        public readonly PowerCellSlotComponent CellSlot = default!;
        public PowerCellComponent? Cell => CellSlot.Cell;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("paralyzeChanceNoSlowdown")]
        public float ParalyzeChanceNoSlowdown = 0.35f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("paralyzeChanceWithSlowdown")]
        public float ParalyzeChanceWithSlowdown = 0.85f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("paralyzeTime")]
        public float ParalyzeTime = 10f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("slowdownTime")]
        public float SlowdownTime = 5f;

        [ViewVariables(VVAccess.ReadWrite)] public float EnergyPerUse { get; set; } = 50;

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!ActionBlockerSystem.CanInteract(eventArgs.User)) return false;
            if (!CellSlot.InsertCell(eventArgs.Using)) return false;
            Dirty();
            return true;
        }
    }
}
