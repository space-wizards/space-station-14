using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.EnergyDome;

/// <summary>
/// component, allows an entity to generate a battery-powered energy dome of a specific type.
/// </summary>
[RegisterComponent, Access(typeof(EnergyDomeSystem))] //Access add
public sealed partial class EnergyDomeGeneratorComponent : Component
{
    [DataField]
    public bool Enabled = false;

    /// <summary>
    /// How much energy will be spent from the battery per unit of damage taken by the shield.
    /// </summary>
    [DataField]
    public float DamageEnergyDraw = 10f;

    /// <summary>
    /// Whether or not the dome can be toggled via standard interactions
    /// (alt verbs, using in hand, etc)
    /// </summary>
    [DataField]
    public bool CanInteractUse = true;

    /// <summary>
    /// Can the NetworkDevice system activate and deactivate the barrier?
    /// </summary>
    [DataField]
    public bool CanDeviceNetworkUse = false;

    //Dome
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId DomePrototype = "EnergyDomeSmallRed";

    [DataField]
    public EntityUid? SpawnedDome;

    /// <summary>
    /// the entity on which the shield will be hung. This is either the container containing
    /// the item or the item itself. Determined when the shield is activated,
    /// it is stored in the component for changing the protected entity.
    /// </summary>
    [DataField]
    public EntityUid? DomeParentEntity;

    //Action
    [DataField]
    public EntProtoId ToggleAction = "ActionToggleDome";

    [DataField]
    public EntityUid? ToggleActionEntity;

    //Sounds
    [DataField]
    public SoundSpecifier AccessDeniedSound = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

    [DataField]
    public SoundSpecifier TurnOnSound = new SoundPathSpecifier("/Audio/Machines/anomaly_sync_connect.ogg");

    [DataField]
    public SoundSpecifier EnergyOutSound = new SoundPathSpecifier("/Audio/Machines/energyshield_down.ogg");

    [DataField]
    public SoundSpecifier TurnOffSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");

    [DataField]
    public SoundSpecifier ParrySound = new SoundPathSpecifier("/Audio/Machines/energyshield_parry.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.05f)
    };

    //Ports
    [DataField]
    public ProtoId<SinkPortPrototype> TogglePort = "Toggle";

    [DataField]
    public ProtoId<SinkPortPrototype> OnPort = "On";

    [DataField]
    public ProtoId<SinkPortPrototype> OffPort = "Off";
}
