using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Power.ApcNetComponents.PowerReceiverUsers;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class EmergencyLightSystem : EntitySystem
    {
        private List<EmergencyLightComponent> _activeLights = new List<EmergencyLightComponent>();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<EmergencyLightMessage>(HandleEmergencyLightMessage);
        }

        private void HandleEmergencyLightMessage(EmergencyLightMessage message)
        {
            switch (message.State)
            {
                case EmergencyLightComponent.EmergencyLightState.Charging:
                    if (_activeLights.Contains(message.Component))
                        _activeLights.Add(message.Component);
                    
                    break;
                case EmergencyLightComponent.EmergencyLightState.Full:
                case EmergencyLightComponent.EmergencyLightState.Empty:
                    _activeLights.Remove(message.Component);
                    break;
                case EmergencyLightComponent.EmergencyLightState.On:
                    if (!_activeLights.Contains(message.Component))
                        _activeLights.Add(message.Component);

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override void Update(float frameTime)
        {
            for (var i = _activeLights.Count - 1; i >= 0; i--)
            {
                var comp = _activeLights[i];
                comp.OnUpdate(frameTime);
            }
        }
    }
}
