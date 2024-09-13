using Content.Server.GatewayStation.Systems;
using Robust.Shared.Audio;

namespace Content.Server.GatewayStation.Components;

/// <summary>
/// TODO
/// </summary>
[RegisterComponent]
[Access(typeof(StationGatewaySystem))]
public sealed partial class StationGatewayComponent : Component
{
    /// <summary>
    /// TODO
    /// </summary>
    [DataField]
    public string GateName = string.Empty;

    /// <summary>
    /// The sound that plays at the portal when it connects to something
    /// </summary>
    [DataField]
    public SoundSpecifier LinkSound = new SoundPathSpecifier("/Audio/Effects/Lightning/lightningbolt.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.05f),
    };

    /// <summary>
    /// The sound played at the portal when it disconnects
    /// </summary>
    [DataField]
    public SoundSpecifier UnlinkSound = new SoundPathSpecifier("/Audio/Effects/gateway_off.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.05f),
    };
}
