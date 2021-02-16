using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Weapon;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Explosion
{
    /// <summary>
    /// When triggered will flash in an area around the object and destroy itself
    /// </summary>
    [RegisterComponent]
    public class FlashExplosiveComponent : Component, ITimerTrigger, IDestroyAct
    {
        public override string Name => "FlashExplosive";

        private float _range;

        private float _duration;

        private string _sound;
        private bool _deleteOnFlash;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _range, "range", 7.0f);
            serializer.DataField(ref _duration, "duration", 8.0f);
            serializer.DataField(ref _sound, "sound", "/Audio/Effects/flash_bang.ogg");
            serializer.DataField(ref _deleteOnFlash, "deleteOnFlash", true);
        }

        public bool Explode()
        {
            // If we're in a locker or whatever then can't flash anything
            Owner.TryGetContainer(out var container);
            if (container == null || !container.Owner.HasComponent<EntityStorageComponent>())
            {
                FlashableComponent.FlashAreaHelper(Owner, _range, _duration);
            }

            if (_sound != null)
            {
                EntitySystem.Get<AudioSystem>().PlayAtCoords(_sound, Owner.Transform.Coordinates);
            }

            if (_deleteOnFlash && !Owner.Deleted)
            {
                Owner.Delete();
            }

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
