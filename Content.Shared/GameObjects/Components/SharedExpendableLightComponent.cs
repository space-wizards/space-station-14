using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components
{
    [Serializable, NetSerializable]
    public enum ExpendableLightVisuals
    {
        State
    }

    [Serializable, NetSerializable]
    public enum ExpendableLightState
    {
        BrandNew,
        Lit,
        Fading,
        Dead
    }

    public abstract class SharedExpendableLightComponent: Component
    {
        public sealed override string Name => "ExpendableLight";

        [ViewVariables(VVAccess.ReadOnly)]
        protected ExpendableLightState CurrentState { get; set; }

        [ViewVariables]
        protected string TurnOnBehaviourID { get; set; }

        [ViewVariables]
        protected string FadeOutBehaviourID { get; set; }

        [ViewVariables]
        protected float GlowDuration { get; set; }

        [ViewVariables]
        protected float FadeOutDuration { get; set; }

        [ViewVariables]
        protected string SpentDesc { get; set; }

        [ViewVariables]
        protected string SpentName { get; set; }

        [ViewVariables]
        protected string IconStateSpent { get; set; }

        [ViewVariables]
        protected string IconStateLit { get; set; }

        [ViewVariables]
        protected string LitSound { get; set; }

        [ViewVariables]
        protected string LoopedSound { get; set; }

        [ViewVariables]
        protected string DieSound { get; set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.TurnOnBehaviourID, "turnOnBehaviourID", string.Empty);
            serializer.DataField(this, x => x.FadeOutBehaviourID, "fadeOutBehaviourID", string.Empty);
            serializer.DataField(this, x => x.GlowDuration, "glowDuration", 60 * 15f);
            serializer.DataField(this, x => x.FadeOutDuration, "fadeOutDuration", 60 * 5f);
            serializer.DataField(this, x => x.SpentName, "spentName", string.Empty);
            serializer.DataField(this, x => x.SpentDesc, "spentDesc", string.Empty);
            serializer.DataField(this, x => x.IconStateLit, "iconStateOn", string.Empty);
            serializer.DataField(this, x => x.IconStateSpent, "iconStateSpent", string.Empty);
            serializer.DataField(this, x => x.LitSound, "litSound", string.Empty);
            serializer.DataField(this, x => x.LoopedSound, "loopedSound", string.Empty);
            serializer.DataField(this, x => x.DieSound, "dieSound", string.Empty);
        }
    }
}
