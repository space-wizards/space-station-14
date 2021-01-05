using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Weapon;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Prototypes;
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

        [YamlField("range")]
        private float _range = 7.0f;
        [YamlField("duration")]
        private float _duration = 8.0f;
        [YamlField("sound")]
        private string _sound = "/Audio/Effects/flash_bang.ogg";
        [YamlField("deleteOnFlash")]
        private bool _deleteOnFlash = true;

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
