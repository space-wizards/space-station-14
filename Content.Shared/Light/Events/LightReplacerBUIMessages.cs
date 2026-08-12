using Content.Shared.Light.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Light.Events;

/// <summary>
/// This message is sent from the client when the player wants to switch the active light bulb.
/// </summary>
/// <param name="light">A mix of the light name and the light bulb type.</param>
[Serializable, NetSerializable]
public sealed class SwitchLightTypeMessage((EntProtoId, LightBulbType) light) : BoundUserInterfaceMessage
{
    public EntProtoId LightEntProtoId = light.Item1;
    public LightBulbType LightType = light.Item2;
}

/// <summary>
/// This message is sent from the client when the player wants to eject all lights of a specific type.
/// </summary>
/// <param name="lightEntProtoId">The name of the lights to be ejected.</param>
[Serializable, NetSerializable]
public sealed class EjectLightTypeMessage(EntProtoId lightEntProtoId) : BoundUserInterfaceMessage
{
    public EntProtoId LightEntProtoId = lightEntProtoId;
}
