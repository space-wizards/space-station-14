using Content.Shared.Arcade.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Arcade.Components;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedBlockGameArcadeSystem))]
[AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class BlockGameArcadeComponent : Component
{
    /// <summary>
    ///
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPhysicsUpdate = TimeSpan.Zero;

    /// <summary>
    ///
    /// </summary>
    [DataField]
    public TimeSpan PhysicsCooldown = TimeSpan.FromSeconds(1);

    /// <summary>
    ///
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextSpawn = TimeSpan.Zero;

    /// <summary>
    ///
    /// </summary>
    [DataField]
    public TimeSpan SpawnCooldown = TimeSpan.FromSeconds(3);

    /// <summary>
    ///
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public Vector2u PlayerPosition;

    /// <summary>
    ///
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public Vector2u Size = new(10u, 30u);

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public BlockGameArcadeCell[] Cells = [];
}
