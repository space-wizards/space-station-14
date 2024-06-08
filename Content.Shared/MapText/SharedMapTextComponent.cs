using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.MapText;

/// <summary>
/// This is used for displaying text in world space
/// </summary>

[NetworkedComponent, Access(typeof(SharedMapTextSystem))]
public abstract partial class SharedMapTextComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string? Text;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Color Color = Color.White;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string FontId = "Default";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int FontSize = 12;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Vector2 Offset = Vector2.Zero;
}

[Serializable, NetSerializable]
public sealed class MapTextComponentState : ComponentState
{
    public string? Text { get; init;}
    public Color Color { get; init;}
    public string FontId { get; init; } = default!;
    public int FontSize { get; init;}
    public Vector2 Offset { get; init;}
}
