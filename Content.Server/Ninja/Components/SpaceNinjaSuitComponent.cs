using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;

namespace Content.Server.Ninja.Components;

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
}

public sealed class TogglePhaseCloakEvent : InstantActionEvent { }
