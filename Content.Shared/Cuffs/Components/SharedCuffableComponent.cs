#nullable enable
using System;
using Content.Shared.ActionBlocker;
using Content.Shared.NetIDs;
using Content.Shared.Pulling.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Cuffs.Components
{
    public class SharedCuffableComponent : Component, IActionBlocker
    {
        public override string Name => "Cuffable";
        public override uint? NetID => ContentNetIDs.CUFFED;

        [ComponentDependency] private readonly SharedPullableComponent? _pullable = default!;

        [ViewVariables]
        public bool CanStillInteract { get; set; } = true;

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

        [Serializable, NetSerializable]
        protected sealed class CuffableComponentState : ComponentState
        {
            public bool CanStillInteract { get; }
            public int NumHandsCuffed { get; }
            public string? RSI { get; }
            public string IconState { get; }
            public Color Color { get; }

            public CuffableComponentState(int numHandsCuffed, bool canStillInteract, string? rsiPath, string iconState, Color color) : base(ContentNetIDs.CUFFED)
            {
                NumHandsCuffed = numHandsCuffed;
                CanStillInteract = canStillInteract;
                RSI = rsiPath;
                IconState = iconState;
                Color = color;
            }
        }
    }
}
