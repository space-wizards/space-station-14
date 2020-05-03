using Content.Server.GameObjects.Components.Weapon;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Explosion
{
    /// <summary>
    /// When triggered will flash in an area around the object and destroy itself
    /// </summary>
    [RegisterComponent]
    public class FlashBangComponent : Component, ITimerTrigger, IDestroyAct
    {
        public override string Name => "FlashBang";

        public float Range => _range;
        private float _range;
        public double Duration => _duration;
        private double _duration;
        private string _sound;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _range, "range", 7.0f);
            serializer.DataField(ref _duration, "duration", 8.0);
            serializer.DataField(ref _sound, "sound", "/Audio/effects/bang.ogg");
        }

        public bool Explode()
        {
            ServerFlashableComponent.FlashAreaHelper(Owner, _range, _duration, _sound);
            Owner.Delete();
            return true;
        }

        bool ITimerTrigger.Trigger(TimerTriggerEventArgs eventArgs)
        {
            return Explode();
        }

        void IDestroyAct.OnDestroy(DestructionEventArgs eventArgs)
        {
            Explode();
        }
    }
}
