using Content.Shared.Light.Component;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.Light.Components
{
    [RegisterComponent]
    [NetworkedComponent]
    public class EmergencyLightComponent : SharedEmergencyLightComponent
    {
    }
}
