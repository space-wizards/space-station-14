using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Ninja.Systems;
using Robust.Shared.Utility;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Component for ninja suit abilities and power consumption.
/// As an implementation detail, dashing with katana is a suit action which isn't ideal.
/// </summary>
[Access(typeof(SharedNinjaSystem))]
[RegisterComponent]
public sealed class SpaceNinjaSuitComponent : Component
{
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
        Priority = -10,
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
}

public sealed class TogglePhaseCloakEvent : InstantActionEvent { }

public sealed class RecallKatanaEvent : InstantActionEvent { }
