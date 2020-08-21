
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.ActionBlocking
{
    public class SharedCuffedComponent : Component, IActionBlocker
    {
        public override string Name => "Cuffed";
        public override uint? NetID => ContentNetIDs.HANDCUFF;

        [ViewVariables]
        public bool CanStillInteract = true;

        #region ActionBlockers

        bool IActionBlocker.CanInteract() => CanStillInteract;
        bool IActionBlocker.CanUse() => CanStillInteract;
        bool IActionBlocker.CanPickup() => CanStillInteract;
        bool IActionBlocker.CanDrop() => CanStillInteract;
        bool IActionBlocker.CanAttack() => CanStillInteract;
        bool IActionBlocker.CanEquip() => CanStillInteract;
        bool IActionBlocker.CanUnequip() => CanStillInteract;

        #endregion

        [Serializable, NetSerializable]
        protected sealed class CuffedComponentState : ComponentState
        {
            public bool CanStillInteract { get; }
            public int NumHandsCuffed { get; }

            public CuffedComponentState(int numHandsCuffed, bool canStillInteract) : base(ContentNetIDs.HANDCUFF)
            {
                NumHandsCuffed = numHandsCuffed;
                CanStillInteract = canStillInteract;
            }
        }
    }
}
