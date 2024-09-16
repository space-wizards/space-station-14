using Content.Server.GatewayStation.Systems;
using Robust.Shared.Audio;

namespace Content.Server.GatewayStation.Components;

/// <summary>
/// Stores a reference to a specific Gateway. Can be inserted into the gateway control console so that the console can control this gateway
/// </summary>
[RegisterComponent]
[Access(typeof(StationGatewaySystem))]
public sealed partial class GatewayChipComponent : Component
{
    [DataField]
    public EntityUid? ConnectedGate;

    [DataField]
    public string ConnectedName = string.Empty;

    /// <summary>
    /// When initialized, it will attempt to contact a random gateway that has the same code. Can be used for pre-created gateways
    /// </summary>
    // Not ProtoId<TagPrototype> because we can random generate this keys for expeditions
    [DataField]
    public string? AutoLinkKey = null;

    /// <summary>
    /// If AutoLinkKey is not empty, but the link failed to be set up, this entity will be automatically deleted
    /// </summary>
    [DataField]
    public bool DeleteOnFailedLink = true;

    [DataField]
    public SoundSpecifier RecordSound = new SoundPathSpecifier("/Audio/Machines/high_tech_confirm.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.05f),
    };
}
