using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;

namespace Content.Server.Ninja.Components;

[RegisterComponent]
public sealed class SpaceNinjaSuitComponent : Component
{
    /// <summary>
    /// True if the user is currently cloaked. Resets when taking suit off.
    /// </summary>
    [DataField("cloaked")]
    public bool Cloaked = false;

    /// <summary>
    /// The action for toggling suit cloak ability
    /// </summary>
    [DataField("toggleCloakAction")]
    public InstantAction ToggleCloakAction = new()
    {
          UseDelay = TimeSpan.FromSeconds(5), // have to plan un/cloaking ahead of time
        DisplayName = "action-name-toggle-cloak",
          Description = "action-desc-toggle-cloak",
          Event = new ToggleCloakEvent()
      };
}

public sealed class ToggleCloakEvent : InstantActionEvent { }
