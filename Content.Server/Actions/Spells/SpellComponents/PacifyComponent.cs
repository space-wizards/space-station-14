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
    public class PacifyComponent : Component, IActionBlocker
    {
        public override string Name => "MagicPacify";

        [ComponentDependency] private readonly SharedPullableComponent? _pullable = default!;

        [ViewVariables]
        public bool CanStillInteract { get; set; } = false;

        #region ActionBlockers

        bool IActionBlocker.CanMove() => true;

        bool IActionBlocker.CanInteract() => false;

        bool IActionBlocker.CanUse() => false;

        bool IActionBlocker.CanThrow() => false;


        bool IActionBlocker.CanEmote() => true;

        bool IActionBlocker.CanAttack() => false;

        bool IActionBlocker.CanEquip() => true;

        bool IActionBlocker.CanUnequip() => true;


        #endregion

    }
}
