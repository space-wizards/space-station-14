using System;
using System.Collections.Generic;
using Content.Server.Light.Components;
using Content.Shared.Light;
using Content.Shared.Light.Component;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Server.Light.EntitySystems
{
    [UsedImplicitly]
    public sealed class EmergencyLightSystem : SharedEmergencyLightSystem
    {
        private readonly HashSet<EmergencyLightComponent> _activeLights = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<EmergencyLightMessage>(HandleEmergencyLightMessage);
            SubscribeLocalEvent<EmergencyLightComponent, ComponentGetState>(GetCompState);
            SubscribeLocalEvent<EmergencyLightComponent, PointLightToggleEvent>(HandleLightToggle);
        }

        private void HandleLightToggle(EntityUid uid, EmergencyLightComponent component, PointLightToggleEvent args)
        {
            if (component.Enabled == args.Enabled) return;
            component.Enabled = args.Enabled;
            component.Dirty();
        }

        private void GetCompState(EntityUid uid, EmergencyLightComponent component, ref ComponentGetState args)
        {
            args.State = new EmergencyLightComponentState(component.Enabled);
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
