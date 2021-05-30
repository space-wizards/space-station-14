using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Power.ApcNetComponents.PowerReceiverUsers;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class EmergencyLightSystem : EntitySystem
    {
        private readonly HashSet<EmergencyLightComponent> _activeLights = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<EmergencyLightMessage>(HandleEmergencyLightMessage);
        }

        private void HandleEmergencyLightMessage(EmergencyLightMessage message)
        {
            switch (message.State)
            {
                case EmergencyLightComponent.EmergencyLightState.On:
                case EmergencyLightComponent.EmergencyLightState.Charging:
                    _activeLights.Add(message.Component);
                    break;
                case EmergencyLightComponent.EmergencyLightState.Full:
                case EmergencyLightComponent.EmergencyLightState.Empty:
                    _activeLights.Remove(message.Component);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override void Update(float frameTime)
        {
            foreach (var activeLight in _activeLights)
            {
                activeLight.OnUpdate(frameTime);
            }
        }
    }
}
