using Content.Shared.Resist.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Resist.Components;

/// <summary>
/// Allows an entity in an open-on-move storage container to resist until it unlocks or un-welds the container.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(ResistLockerSystem))]
public sealed partial class ResistLockerComponent : Component
{
    /// <summary>
    /// How long, it takes to resist the container.
    /// </summary>
    [DataField]
    public TimeSpan ResistTime = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Whether an escape attempt is currently in progress.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool IsResisting;
}
