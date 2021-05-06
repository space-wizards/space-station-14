using System;
using Content.Shared.GameObjects.Components.Pulling;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Actions
{
    [DataDefinition]
    [RegisterComponent]
    public class StatueComponent : Component, IActionBlocker
    {
        public override string Name => "MagicStatue";

        [ComponentDependency] private readonly SharedPullableComponent? _pullable = default!;

        [ViewVariables]
        public bool CanStillInteract { get; set; } = false;

        #region ActionBlockers

        bool IActionBlocker.CanInteract() => CanStillInteract;
        bool IActionBlocker.CanUse() => CanStillInteract;
        bool IActionBlocker.CanPickup() => CanStillInteract;
        bool IActionBlocker.CanDrop() => CanStillInteract;
        bool IActionBlocker.CanAttack() => CanStillInteract;
        bool IActionBlocker.CanEquip() => CanStillInteract;
        bool IActionBlocker.CanUnequip() => CanStillInteract;
        bool IActionBlocker.CanMove() => _pullable == null || !_pullable.BeingPulled || CanStillInteract;

        #endregion

    }
}
