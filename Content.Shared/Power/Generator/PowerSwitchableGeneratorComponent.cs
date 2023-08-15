using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Power.Generator;

/// <summary>
/// Enables a generator to switch between HV and MV output.
/// </summary>
/// <remarks>
/// Must have <c>CableDeviceNode</c>s for both <c>output_mv</c> and <c>output_hv</c>, and also a <c>PowerSupplierComponent</c>.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedPowerSwitchableGeneratorSystem))]
public sealed partial class PowerSwitchableGeneratorComponent : Component
{
    /// <summary>
    /// Which output the portable generator is currently connected to.
    /// </summary>
    [DataField("activeOutput")]
    [AutoNetworkedField]
    public PowerSwitchableGeneratorOutput ActiveOutput { get; set; }

    /// <summary>
    /// Sound that plays when the output is switched.
    /// </summary>
    /// <returns></returns>
    [DataField("switchSound")]
    public SoundSpecifier? SwitchSound { get; set; }
}

/// <summary>
/// Possible power output for power-switchable generators.
/// </summary>
/// <seealso cref="PowerSwitchableGeneratorComponent"/>
[Serializable, NetSerializable]
public enum PowerSwitchableGeneratorOutput : byte
{
    /// <summary>
    /// The generator is set to connect to a high-voltage power network.
    /// </summary>
    HV,

    /// <summary>
    /// The generator is set to connect to a medium-voltage power network.
    /// </summary>
    MV
}

