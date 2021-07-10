using Content.Server.Flash.Components;
using Content.Server.Storage.Components;
using Content.Shared.Acts;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Explosion.Components
{
    /// <summary>
    /// When triggered will flash in an area around the object and destroy itself
    /// </summary>
    [RegisterComponent]
    public class FlashExplosiveComponent : Component, ITimerTrigger, IDestroyAct
    {
        public override string Name => "FlashExplosive";

        [DataField("range")]
        private float _range = 7.0f;
        [DataField("duration")]
        private float _duration = 8.0f;
        [DataField("sound")]
        private SoundSpecifier _sound = new SoundPathSpecifier("/Audio/Effects/flash_bang.ogg");
        [DataField("deleteOnFlash")]
        private bool _deleteOnFlash = true;

        public bool Explode()
        {
            // If we're in a locker or whatever then can't flash anything
            Owner.TryGetContainer(out var container);
            if (container == null || !container.Owner.HasComponent<EntityStorageComponent>())
            {
                FlashableComponent.FlashAreaHelper(Owner, _range, _duration);
            }

            if (_sound.TryGetSound(out var sound))
            {
                SoundSystem.Play(Filter.Pvs(Owner), sound, Owner.Transform.Coordinates);
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
