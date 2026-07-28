using Robust.Shared.GameStates;

namespace Content.Shared.Bible.Components;

/// <summary>
/// This component is for the Bible user's familiars, and mostly
/// used to track their current state and to give a component to check for
/// if any special behavior is needed.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(BibleSystem))]
public sealed partial class FamiliarComponent : Component
{
    /// <summary>
    /// The entity this familiar was summoned from.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Source = null;
}
