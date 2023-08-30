using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.DoAfter;
using Content.Shared.Ninja.Systems;
using Content.Shared.Toggleable;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Component for toggling glove powers.
/// Powers being enabled is controlled by User not being null.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedNinjaGlovesSystem))]
public sealed partial class NinjaGlovesComponent : Component
{
    /// <summary>
    /// Entity of the ninja using these gloves, usually means enabled
    /// </summary>
    [DataField("user"), AutoNetworkedField]
    public EntityUid? User;

    /// <summary>
    /// The action for toggling ninja gloves abilities
    /// </summary>
    [DataField("toggleAction")]
    public InstantAction ToggleAction = new()
    {
        DisplayName = "action-name-toggle-ninja-gloves",
        Description = "action-desc-toggle-ninja-gloves",
        Priority = -13,
        Event = new ToggleActionEvent()
    };

    /// <summary>
    /// The whitelist used for the emag provider to emag airlocks only (not regular doors).
    /// </summary>
    [DataField("doorjackWhitelist")]
    public EntityWhitelist DoorjackWhitelist = new()
    {
        Components = new[] {"Airlock"}
    };
}
