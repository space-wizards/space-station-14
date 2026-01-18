using Content.Shared.Arcade.Systems;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.GameStates;

namespace Content.Shared.Arcade.Components;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedArcadeRewardsSystem))]
[AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class ArcadeRewardsComponent : Component
{
    /// <summary>
    ///
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public EntityTableSelector Rewards = default!;

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public byte Amount;

    /// <summary>
    ///
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public byte MaxAmount;

    /// <summary>
    ///
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public byte MinAmount;
}
