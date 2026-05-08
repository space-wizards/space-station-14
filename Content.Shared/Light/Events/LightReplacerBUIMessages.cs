using Content.Shared.Light.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Light.Events;

/// <summary>
/// This message is sent from the client when the player wants to switch the active light bulb.
/// </summary>
/// <param name="light">A mix of the light name and the light bulb type.</param>
[Serializable, NetSerializable]
public sealed class SwitchLightTypeMessage((string, LightBulbType) light) : BoundUserInterfaceMessage
{
    public string LightName = light.Item1;
    public LightBulbType LightType = light.Item2;
}

/// <summary>
/// This message is sent from the client when the player wants to eject all lights of a specific type.
/// </summary>
/// <param name="lightName">The name of the lights to be ejected.</param>
[Serializable, NetSerializable]
public sealed class EjectLightTypeMessage(string lightName) : BoundUserInterfaceMessage
{
    public string LightName = lightName;
}
