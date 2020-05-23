using Content.Server.GameObjects.Components.Interactable.Tools;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Utility;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class WrenchableComponent : Component, IInteractUsing
    {
        public override string Name => "Wrenchable";
        private AudioSystem _audioSystem;

        public override void Initialize()
        {
            base.Initialize();
            _audioSystem = EntitySystem.Get<AudioSystem>();
        }

        public bool InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.Using.HasComponent<WrenchComponent>())
            {
                return false;
            }

            if (!Owner.TryGetComponent(out PhysicsComponent physics))
            {
                return false;
            }

            physics.Anchored = !physics.Anchored;
            _audioSystem.Play("/Audio/items/ratchet.ogg", Owner);

            return true;
        }
    }
}
