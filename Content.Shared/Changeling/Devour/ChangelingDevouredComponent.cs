using Robust.Shared.GameStates;

namespace Content.Shared.Changeling.Devour;

/// <summary>
/// Component used for marking entities devoured by a changeling.
/// Used to prevent granting the identity several times.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChangelingDevouredComponent : Component
{
    /// <summary>
    /// List of all changelings that have devoured this entity.
    /// </summary>
    // TODO: Delete this component once we have WeakEntityReference.
    [DataField, AutoNetworkedField]
    public List<EntityUid> DevouredBy = new();

    // Prevent other clients from knowing whos identity has been stolen.
    public override bool SendOnlyToOwner => true;
}
