using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.EnergyDome;

[RegisterComponent, Access(typeof(EnergyDomeSystem))] //Access add
public sealed partial class EnergyDomeGeneratorComponent : Component
{
    /// <summary>
    /// Amount of energy input per second
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Wattage = 20f;

    /// <summary>
    /// After an emergency shutdown, the dome goes to recharge, during which time it cannot be turned back on
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public double ReloadSecond = 10;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextActivation;

    [DataField]
    public bool Enabled = false;

    [DataField]
    public bool Powered = false;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId DomePrototype = "EnergyDomeBase";

    [DataField]
    public EntityUid? SpawnedDome;

    [DataField]
    public SoundSpecifier AccessDeniedSound = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

    [DataField]
    public SoundSpecifier TurnOnSound = new SoundPathSpecifier("/Audio/Machines/anomaly_sync_connect.ogg");

    [DataField]
    public SoundSpecifier TurnOffSound = new SoundPathSpecifier("/Audio/Machines/energyshield_down.ogg");

    [DataField]
    public SoundSpecifier NoPowerSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");
}
