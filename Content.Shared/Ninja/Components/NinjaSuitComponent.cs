using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Ninja.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Shared.Ninja.Components;

// TODO: ResourcePath -> ResPath when thing gets merged

/// <summary>
/// Component for ninja suit abilities and power consumption.
/// As an implementation detail, dashing with katana is a suit action which isn't ideal.
/// </summary>
[Access(typeof(SharedNinjaSuitSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NinjaSuitComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public bool Cloaked = false;

    /// <summary>
    /// The action for toggling suit phase cloak ability
    /// </summary>
    [DataField("togglePhaseCloakAction")]
    public InstantAction TogglePhaseCloakAction = new()
    {
        UseDelay = TimeSpan.FromSeconds(5), // have to plan un/cloaking ahead of time
        DisplayName = "action-name-toggle-phase-cloak",
        Description = "action-desc-toggle-phase-cloak",
        Priority = -9,
        Event = new TogglePhaseCloakEvent()
    };

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
    /// The action for creating throwing soap, in place of ninja throwing stars since embedding doesn't exist.
    /// </summary>
    [DataField("createSoapAction")]
    public InstantAction CreateSoapAction = new()
    {
        UseDelay = TimeSpan.FromSeconds(10),
        Icon = new SpriteSpecifier.Rsi(new ResourcePath("Objects/Specific/Janitorial/soap.rsi"), "soap"),
        ItemIconStyle = ItemActionIconStyle.NoItem,
        DisplayName = "action-name-create-soap",
        Description = "action-desc-create-soap",
        Priority = -10,
        Event = new CreateSoapEvent()
    };

    /// <summary>
    /// Battery charge used to create a throwing soap. Can do it 25 times on a small-capacity power cell.
    /// </summary>
    [DataField("soapCharge")]
    public float SoapCharge = 14.4f;

    /// <summary>
    /// Soap item to create with the action
    /// </summary>
    [DataField("soapPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SoapPrototype = "SoapNinja";

    /// <summary>
    /// The action for recalling a bound energy katana
    /// </summary>
    [DataField("recallkatanaAction")]
    public InstantAction RecallKatanaAction = new()
    {
        UseDelay = TimeSpan.FromSeconds(1),
        Icon = new SpriteSpecifier.Rsi(new ResourcePath("Objects/Weapons/Melee/energykatana.rsi"), "icon"),
        ItemIconStyle = ItemActionIconStyle.NoItem,
        DisplayName = "action-name-recall-katana",
        Description = "action-desc-recall-katana",
        Priority = -11,
        Event = new RecallKatanaEvent()
    };

    /// <summary>
    /// The action for dashing somewhere using katana
    /// </summary>
    [DataField("katanaDashAction")]
    public WorldTargetAction KatanaDashAction = new()
    {
        Icon = new SpriteSpecifier.Rsi(new ResourcePath("Objects/Magic/magicactions.rsi"), "blink"),
        ItemIconStyle = ItemActionIconStyle.NoItem,
        DisplayName = "action-name-katana-dash",
        Description = "action-desc-katana-dash",
        Priority = -12,
        Event = new KatanaDashEvent(),
        // doing checks manually
        CheckCanAccess = false,
        Range = 0f
    };

    /// <summary>
    /// The action for creating an EMP burst
    /// </summary>
    [DataField("empAction")]
    public InstantAction EmpAction = new()
    {
        Icon = new SpriteSpecifier.Rsi(new ResourcePath("Objects/Weapons/Grenades/empgrenade.rsi"), "icon"),
        ItemIconStyle = ItemActionIconStyle.BigAction,
        DisplayName = "action-name-em-burst",
        Description = "action-desc-em-burst",
        Priority = -13,
        Event = new NinjaEmpEvent()
    };

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
}

public sealed class TogglePhaseCloakEvent : InstantActionEvent { }

public sealed class CreateSoapEvent : InstantActionEvent { }

public sealed class RecallKatanaEvent : InstantActionEvent { }

public sealed class NinjaEmpEvent : InstantActionEvent { }
