using Content.Shared.Arcade.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
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
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan UpdateCooldown = TimeSpan.FromSeconds(0.5);

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public BlockGameArcadeMove MoveDirection = BlockGameArcadeMove.None;

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2i Size = new(12, 5);

    /// <summary>
    ///
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public uint BufferWidth = 5;

    /// <summary>
    ///
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public uint NextBagPiece = 0;

    /// <summary>
    ///
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<BlockGamePiecePrototype>[] PiecesBag;

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public BlockGameArcadeCell[]? FallingPiece;

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public BlockGameArcadeCell[] Cells = [];
}
