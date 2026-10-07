using Content.Shared.DeviceLinking.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceLinking.Components;

/// <summary>
/// A system that allows you to control <see cref="GunComponent"/> and
/// <see cref="AmmoProviderComponent"/> using Device linking.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(GunSignalControlSystem))]
public sealed partial class GunSignalControlComponent : Component
{
    /// <summary>
    /// Input port that triggers the gun.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> TriggerPort = "Trigger";

    /// <summary>
    /// Input port that toggles the gun's enabled state.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> TogglePort = "Toggle";

    /// <summary>
    /// Input port that enables the gun.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> OnPort = "On";

    /// <summary>
    /// Input port that disables the gun.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> OffPort = "Off";
}
