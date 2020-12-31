#nullable enable
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Prototypes.Cargo;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Timers;
using Robust.Shared.GameObjects.Systems;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.Cargo
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


        public override void OnAdd()
        {
            base.OnAdd();

            var receiver = Owner.EnsureComponent<PowerReceiverComponent>();
            receiver.OnPowerStateChanged += PowerUpdate;
        }

        public override void OnRemove()
        {
            if (Owner.TryGetComponent(out PowerReceiverComponent? receiver))
            {
                receiver.OnPowerStateChanged -= PowerUpdate;
            }
            base.OnRemove();
        }

        public void QueueTeleport(CargoProductPrototype product)
        {
            _teleportQueue.Add(product);
            TeleportLoop();
        }

        private void PowerUpdate(object? sender, PowerStateEventArgs args)
        {
            if (args.Powered && _currentState == CargoTelepadState.Unpowered) {
                _currentState = CargoTelepadState.Idle;
                if(Owner.TryGetComponent<SpriteComponent>(out var spriteComponent))
                    spriteComponent.LayerSetState(0, "pad-idle");
                TeleportLoop();
            }
            else if (!args.Powered)
            {
                _currentState = CargoTelepadState.Unpowered;
                if (Owner.TryGetComponent<SpriteComponent>(out var spriteComponent))
                    spriteComponent.LayerSetState(0, "pad-offline");
            }
        }
        private void TeleportLoop()
        {
            if (_currentState == CargoTelepadState.Idle && _teleportQueue.Count > 0)
            {
                _currentState = CargoTelepadState.Charging;
                if (Owner.TryGetComponent<SpriteComponent>(out var spriteComponent))
                    spriteComponent.LayerSetState(0, "pad-idle");
                Owner.SpawnTimer((int) (TeleportDelay * 1000), () =>
                {
                    if (!Deleted && !Owner.Deleted && _currentState == CargoTelepadState.Charging && _teleportQueue.Count > 0)
                    {
                        _currentState = CargoTelepadState.Teleporting;
                        if (Owner.TryGetComponent<SpriteComponent>(out var spriteComponent))
                            spriteComponent.LayerSetState(0, "pad-beam");
                        Owner.SpawnTimer((int) (TeleportDuration * 1000), () =>
                        {
                            if (!Deleted && !Owner.Deleted && _currentState == CargoTelepadState.Teleporting && _teleportQueue.Count > 0)
                            {
                                EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Machines/phasein.ogg", Owner, AudioParams.Default.WithVolume(-8f));
                                Owner.EntityManager.SpawnEntity(_teleportQueue[0].Product, Owner.Transform.Coordinates);
                                _teleportQueue.RemoveAt(0);
                                if (Owner.TryGetComponent<SpriteComponent>(out var spriteComponent))
                                    spriteComponent.LayerSetState(0, "pad-idle");
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
