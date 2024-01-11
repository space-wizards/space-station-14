using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.EnergyDome;

/// <summary>
/// component, allows an entity to generate a battery-powered energy dome of a specific type.
/// </summary>
[RegisterComponent, Access(typeof(EnergyDomeSystem))] //Access add
public sealed partial class EnergyDomeGeneratorComponent : Component
{
    /// <summary>
    /// Amount of energy input per second
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Wattage = 20f;

    /// <summary>
    /// How much energy will be spent from the battery per unit of damage taken by the shield.
    /// </summary>
    [DataField]
    public float EnergyLessForDamage = 50f;

    /// <summary>
    /// After an emergency shutdown, the dome goes to recharge, during which time it cannot be turned back on
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public double ReloadSecond = 10;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextActivation;

    /// <summary>
    /// Whether or not the light can be toggled via standard interactions
    /// (alt verbs, using in hand, etc)
    /// </summary>
    [DataField]
    public bool ToggleOnInteract = true;

    [DataField]
    public bool Enabled = false;

    //Dome
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId DomePrototype = "EnergyDomeBase";
    [DataField]
    public EntityUid? SpawnedDome;

    //Action
    [DataField]
    public EntProtoId ToggleAction = "ActionToggleDome";
    [DataField]
    public EntityUid? ToggleActionEntity;

    [DataField]
    public SoundSpecifier AccessDeniedSound = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

    [DataField]
    public SoundSpecifier TurnOnSound = new SoundPathSpecifier("/Audio/Machines/anomaly_sync_connect.ogg");

    [DataField]
    public SoundSpecifier EnergyOutSound = new SoundPathSpecifier("/Audio/Machines/energyshield_down.ogg");

    [DataField]
    public SoundSpecifier TurnOffSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");

    [DataField]
    public SoundSpecifier ParrySound = new SoundPathSpecifier("/Audio/Machines/energyshield_parry.ogg");
}
