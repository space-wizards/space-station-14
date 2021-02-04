using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds.Behavior
{
    public class DoActsBehavior : IThresholdBehavior
    {
        private int _acts;

        /// <summary>
        ///     What acts should be triggered upon activation.
        ///     See <see cref="ActSystem"/>.
        /// </summary>
        public ThresholdActs Acts
        {
            get => (ThresholdActs) _acts;
            set => _acts = (int) value;
        }

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _acts, "acts", 0, WithFormat.Flags<ActsFlags>());
        }

        public bool HasAct(ThresholdActs act)
        {
            return (_acts & (int) act) != 0;
        }

        public void Trigger(IEntity owner, DestructibleSystem system)
        {
            if (HasAct(ThresholdActs.Breakage))
            {
                system.ActSystem.HandleBreakage(owner);
            }

            if (HasAct(ThresholdActs.Destruction))
            {
                system.ActSystem.HandleDestruction(owner);
            }
        }
    }
}
