using Content.Shared.Arcade.Systems;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.GameStates;

namespace Content.Shared.Arcade.Components;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedArcadeRewardsSystem))]
[AutoGenerateComponentState]
public sealed partial class ArcadeRewardsComponent : Component
{
    /// <summary>
    ///
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector Rewards;

    /// <summary>
    ///
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public int MinAmount;

    /// <summary>
    ///
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public int MaxAmount;

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Amount;
}
