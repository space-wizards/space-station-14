using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.DeviceLinking.Components;

/// <summary>
/// A system that allows you to fire GunComponent + AmmoProvider by receiving signals from DeviceLinking
/// </summary>
[RegisterComponent, Access(typeof(GunSignalControlSystem))]
public sealed partial class GunSignalControlComponent : Component
{
    /// <summary>
    /// for automatic weapons. Determines if the gun is attempting to fire without stopping
    /// </summary>
    [DataField]
    public bool Enabled = false;

    [DataField(customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextShootTime;

    [DataField]
    public ProtoId<SinkPortPrototype> TriggerPort = "Trigger";

    [DataField]
    public ProtoId<SinkPortPrototype> TogglePort = "Toggle";

    [DataField]
    public ProtoId<SinkPortPrototype> OnPort = "On";

    [DataField]
    public ProtoId<SinkPortPrototype> OffPort = "Off";
}
