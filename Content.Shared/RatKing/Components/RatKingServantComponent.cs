using Content.Shared.RatKing.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.RatKing.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(RatKingSystem))]
[AutoGenerateComponentState]
public sealed partial class RatKingServantComponent : Component
{
    /// <summary>
    /// The king this rat belongs to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? King;
}
