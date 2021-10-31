using System;
using System.Collections.Generic;
using Content.Server.Power.Components;
using Content.Shared.Cargo;
using Content.Shared.Sound;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Cargo.Components
{

    //This entire class is a PLACEHOLDER for the cargo shuttle.

    [RegisterComponent]
    public class CargoTelepadComponent : Component
    {
        public override string Name => "CargoTelepad";

        private const float TeleportDuration = 0.5f;
        private const float TeleportDelay = 15f;
        private List<CargoProductPrototype> _teleportQueue = new List<CargoProductPrototype>();
        private CargoTelepadState _currentState = CargoTelepadState.Unpowered;
        [DataField("teleportSound")] private SoundSpecifier _teleportSound = new SoundPathSpecifier("/Audio/Machines/phasein.ogg");

        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
#pragma warning disable 618
            base.HandleMessage(message, component);
#pragma warning restore 618
            switch (message)
            {
                case PowerChangedMessage powerChanged:
                    PowerUpdate(powerChanged);
                    break;
            }
        }

        public void QueueTeleport(CargoProductPrototype product)
        {
            _teleportQueue.Add(product);
            TeleportLoop();
        }

        private void PowerUpdate(PowerChangedMessage args)
        {
            if (args.Powered && _currentState == CargoTelepadState.Unpowered) {
                _currentState = CargoTelepadState.Idle;
                if(Owner.TryGetComponent<SpriteComponent>(out var spriteComponent) && spriteComponent.LayerCount > 0)
                    spriteComponent.LayerSetState(0, "idle");
                TeleportLoop();
            }
            else if (!args.Powered)
            {
                _currentState = CargoTelepadState.Unpowered;
                if (Owner.TryGetComponent<SpriteComponent>(out var spriteComponent) && spriteComponent.LayerCount > 0)
                    spriteComponent.LayerSetState(0, "offline");
            }
        }
        private void TeleportLoop()
        {
            if (_currentState == CargoTelepadState.Idle && _teleportQueue.Count > 0)
            {
                _currentState = CargoTelepadState.Charging;
                if (Owner.TryGetComponent<SpriteComponent>(out var spriteComponent) && spriteComponent.LayerCount > 0)
                    spriteComponent.LayerSetState(0, "idle");
                Owner.SpawnTimer((int) (TeleportDelay * 1000), () =>
                {
                    if (!Deleted && !Owner.Deleted && _currentState == CargoTelepadState.Charging && _teleportQueue.Count > 0)
                    {
                        _currentState = CargoTelepadState.Teleporting;
                        if (Owner.TryGetComponent<SpriteComponent>(out var spriteComponent) && spriteComponent.LayerCount > 0)
                            spriteComponent.LayerSetState(0, "beam");
                        Owner.SpawnTimer((int) (TeleportDuration * 1000), () =>
                        {
                            if (!Deleted && !Owner.Deleted && _currentState == CargoTelepadState.Teleporting && _teleportQueue.Count > 0)
                            {
                                SoundSystem.Play(Filter.Pvs(Owner), _teleportSound.GetSound(), Owner, AudioParams.Default.WithVolume(-8f));
                                Owner.EntityManager.SpawnEntity(_teleportQueue[0].Product, Owner.Transform.Coordinates);
                                _teleportQueue.RemoveAt(0);
                                if (Owner.TryGetComponent<SpriteComponent>(out var spriteComponent) && spriteComponent.LayerCount > 0)
                                    spriteComponent.LayerSetState(0, "idle");
                                _currentState = CargoTelepadState.Idle;
                                TeleportLoop();
                            }
                        });
                    }
                });
            }
        }

        private enum CargoTelepadState { Unpowered, Idle, Charging, Teleporting };
    }
}
