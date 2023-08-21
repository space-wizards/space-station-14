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
using System.Threading;

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
    [ViewVariables, AutoNetworkedField]
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

/// <summary>
/// Component for stealing technologies from a R&D server, when gloves are enabled.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class ResearchStealerComponent : Component
{
    /// <summary>
    /// Time taken to steal research from a server
    /// </summary>
    [DataField("delay")]
    public TimeSpan Delay = TimeSpan.FromSeconds(20);
}

/// <summary>
/// Component for hacking a communications console to call in a threat.
/// Can only be done once, the component is remove afterwards.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class CommsHackerComponent : Component
{
    /// <summary>
    /// Time taken to hack the console
    /// </summary>
    [DataField("delay")]
    public TimeSpan Delay = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Possible threats to choose from.
    /// </summary>
    [DataField("threats")]
    public List<String> Threats
}

/// <summary>
/// DoAfter event for drain ability.
/// </summary>
[Serializable, NetSerializable]
public sealed class DrainDoAfterEvent : SimpleDoAfterEvent { }

/// <summary>
/// DoAfter event for research stealing ability.
/// </summary>
[Serializable, NetSerializable]
public sealed class ResearchStealDoAfterEvent : SimpleDoAfterEvent { }

/// <summary>
/// DoAfter event for comms console terror ability.
/// </summary>
[Serializable, NetSerializable]
public sealed class TerrorDoAfterEvent : SimpleDoAfterEvent { }
