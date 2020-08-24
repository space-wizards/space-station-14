using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components
{
    public abstract class SharedGlowstickComponent: Component
    {
        protected enum GlowstickState
        {
            BrandNew,
            Lit,
            Fading,
            Dead
        }

        public sealed override string Name => "Glowstick";
        public sealed override uint? NetID => ContentNetIDs.GLOWSTICK;

        [ViewVariables(VVAccess.ReadOnly)]
        protected GlowstickState CurrentState { get; set; }

        [ViewVariables]
        protected float GlowRadius { get; set; }

        [ViewVariables]
        protected float GlowEnergy { get; set; }

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

        [Serializable, NetSerializable]
        protected sealed class GlowstickComponentState : ComponentState
        {
            public GlowstickComponentState(GlowstickState state, float stateExpiryTime) : base(ContentNetIDs.GLOWSTICK)
            {
                StateExpiryTime = stateExpiryTime;
                State = state;
            }

            public float StateExpiryTime { get; set; }
            public GlowstickState State { get; set; }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => GlowRadius, "glowRadius", 3);
            serializer.DataField(this, x => GlowEnergy, "glowEnergy", 3);
            serializer.DataField(this, x => GlowDuration, "glowDuration", 60 * 15);
            serializer.DataField(this, x => FadeOutDuration, "fadeOutDuration", 60 * 5);
            serializer.DataField(this, x => SpentName, "spentName", string.Empty);
            serializer.DataField(this, x => SpentDesc, "spentDesc", string.Empty);
            serializer.DataField(this, x => IconStateLit, "iconStateOn", string.Empty);
            serializer.DataField(this, x => IconStateSpent, "iconStateSpent", string.Empty);
        }
    }
}
