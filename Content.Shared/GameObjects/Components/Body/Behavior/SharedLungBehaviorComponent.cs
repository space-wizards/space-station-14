#nullable enable
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Body.Behavior
{
    public abstract class SharedLungBehaviorComponent : MechanismBehaviorComponent
    {
        public override string Name => "Lung";

        [ViewVariables] public abstract float Temperature { get; }

        [ViewVariables] public abstract float Volume { get; }

        [ViewVariables] public LungStatus Status { get; set; }

        [ViewVariables] public float CycleDelay { get; set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, l => l.CycleDelay, "cycleDelay", 2);
        }

        public abstract void Inhale(float frameTime);

        public abstract void Exhale(float frameTime);

        public abstract void Gasp();
    }

    public enum LungStatus
    {
        None = 0,
        Inhaling,
        Exhaling
    }
}
