using Content.Shared.Arcade.Enums;
using Content.Shared.Arcade.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Arcade.Components;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedKudzuCrushArcadeSystem))]
[AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class KudzuCrushArcadeComponent : Component
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
    public KudzuCrushArcadeAction NextAction;

    /// <summary>
    ///
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public Vector2i GridSize = new(14, 24);

    /// <summary>
    ///
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public int[]? FallingPieceCells;

    /// <summary>
    ///
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public int NextBagPiece = 0;

    /// <summary>
    ///
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<KudzuCrushPiecePrototype>[] PiecesBag;

    /// <summary>
    ///
    /// </summary>
    [DataField, AutoNetworkedField]
    public KudzuCrushArcadeCell[] Grid = [];
}
