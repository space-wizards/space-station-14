using Content.Shared.Ninja.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Component for draining power from APCs/substations/SMESes, when ProviderUid is set to a battery cell.
/// Does not rely on relay, simply being on the user and having BatteryUid set is enough.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedBatteryDrainerSystem))]
public sealed partial class BatteryDrainerComponent : Component
{
    /// <summary>
    /// The powercell entity to drain power into.
    /// Determines whether draining is possible.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? BatteryUid;

    /// <summary>
    /// Conversion rate between joules in a device and joules added to battery.
    /// Should be very low since powercells store nothing compared to even an APC.
    /// </summary>
    [DataField]
    public float DrainEfficiency = 0.001f;

    /// <summary>
    /// Time that the do after takes to drain charge from a battery, in seconds
    /// </summary>
    [DataField]
    public float DrainTime = 1f;

    /// <summary>
    /// Sound played after the doafter ends.
    /// </summary>
    [DataField]
    public SoundSpecifier SparkSound = new SoundCollectionSpecifier("sparks");
}
