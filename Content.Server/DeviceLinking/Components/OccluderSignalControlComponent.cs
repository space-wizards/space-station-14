using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server.DeviceLinking.Components;

/// <summary>
/// Component for controlling an occluder via device linking signals.
/// Allows enabling, disabling, or toggling the occluder state.
/// <seealso cref="OccluderSignalControlSystem"/>
/// </summary>
[RegisterComponent, Access(typeof(OccluderSignalControlSystem))]
public sealed partial class OccluderSignalControlComponent : Component
{
    /// <summary>
    /// Name of the port used to enable the occluder.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> EnablePort = "On";

    /// <summary>
    /// Name of the port used to disable the occluder.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> DisablePort = "Off";

    /// <summary>
    /// Name of the port used to toggle the occluder state.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> TogglePort = "Toggle";
}
