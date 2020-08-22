
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;
using System;

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
            public string RSI { get; }
            public string IconState { get; }
            public Color Color { get; }

            public CuffedComponentState(int numHandsCuffed, bool canStillInteract, string rsiPath, string iconState, Color color) : base(ContentNetIDs.HANDCUFF)
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
