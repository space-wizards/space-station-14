using Content.Shared.Actions;
using Content.Shared.Ninja.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Component for ninja suit abilities and power consumption.
/// As an implementation detail, dashing with katana is a suit action which isn't ideal.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedNinjaSuitSystem))]
public sealed partial class NinjaSuitComponent : Component
{
    /// <summary>
    /// Battery charge used passively, in watts. Will last 1000 seconds on a small-capacity power cell.
    /// </summary>
    [DataField("passiveWattage")]
    public float PassiveWattage = 0.36f;

    /// <summary>
    /// Battery charge used while cloaked, stacks with passive. Will last 200 seconds while cloaked on a small-capacity power cell.
    /// </summary>
    [DataField("cloakWattage")]
    public float CloakWattage = 1.44f;

    /// <summary>
    /// Sound played when a ninja is hit while cloaked.
    /// </summary>
    [DataField("revealSound")]
    public SoundSpecifier RevealSound = new SoundPathSpecifier("/Audio/Effects/chime.ogg");

    /// <summary>
    /// How long to disable all abilities for when revealed.
    /// This adds a UseDelay to the ninja so it should not be set by anything else.
    /// </summary>
    [DataField("disableTime")]
    public TimeSpan DisableTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The action id for creating throwing stars.
    /// </summary>
    [DataField("createThrowingStarAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string CreateThrowingStarAction = "ActionCreateThrowingStar";

    [DataField("createThrowingStarActionEntity")]
    public EntityUid? CreateThrowingStarActionEntity;

    /// <summary>
    /// Battery charge used to create a throwing star. Can do it 25 times on a small-capacity power cell.
    /// </summary>
    [DataField("throwingStarCharge")]
    public float ThrowingStarCharge = 14.4f;

    /// <summary>
    /// Throwing star item to create with the action
    /// </summary>
    [DataField("throwingStarPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ThrowingStarPrototype = "ThrowingStarNinja";

    /// <summary>
    /// The action id for recalling a bound energy katana
    /// </summary>
    [DataField("recallKatanaAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string RecallKatanaAction = "ActionRecallKatana";

    [DataField("recallKatanaActionEntity")]
    public EntityUid? RecallKatanaActionEntity;

    /// <summary>
    /// Battery charge used per tile the katana teleported.
    /// Uses 1% of a default battery per tile.
    /// </summary>
    [DataField("recallCharge")]
    public float RecallCharge = 3.6f;

    /// <summary>
    /// The action id for creating an EMP burst
    /// </summary>
    [DataField("empAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string EmpAction = "ActionNinjaEmp";

    [DataField("empActionEntity")]
    public EntityUid? EmpActionEntity;

    /// <summary>
    /// Battery charge used to create an EMP burst. Can do it 2 times on a small-capacity power cell.
    /// </summary>
    [DataField("empCharge")]
    public float EmpCharge = 180f;

    /// <summary>
    /// Range of the EMP in tiles.
    /// </summary>
    [DataField("empRange")]
    public float EmpRange = 6f;

    /// <summary>
    /// Power consumed from batteries by the EMP
    /// </summary>
    [DataField("empConsumption")]
    public float EmpConsumption = 100000f;

    /// <summary>
    /// How long the EMP effects last for, in seconds
    /// </summary>
    [DataField("empDuration")]
    public float EmpDuration = 60f;
}

public sealed partial class CreateThrowingStarEvent : InstantActionEvent
{
}

public sealed partial class RecallKatanaEvent : InstantActionEvent
{
}

public sealed partial class NinjaEmpEvent : InstantActionEvent
{
}
