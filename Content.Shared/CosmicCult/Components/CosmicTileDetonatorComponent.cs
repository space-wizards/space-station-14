using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.CosmicCult.Components;

/// <summary>
/// Component for Cosmic Cult's entropic colossus' Sunder ability.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause]
public sealed partial class CosmicTileDetonatorComponent : Component
{
    [AutoPausedField, DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan DetonationTimer = default!;

    [DataField] public EntProtoId TileDetonation = "MobTileDamageArea";

    [DataField] public TimeSpan DetonateWait = TimeSpan.FromSeconds(0.525);

    [DataField] public Vector2i DetonationCenter;

    [DataField] public Vector2 MaxSize = new Vector2(8, 8);

    [DataField] public Vector2 Size = new Vector2(0, 0);
}
