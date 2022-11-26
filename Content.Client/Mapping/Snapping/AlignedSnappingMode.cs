using Robust.Client.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager;

namespace Content.Client.Mapping.Snapping;

public sealed class AlignedSnappingMode : SnappingModeImpl
{
    [Dependency] private readonly IEntityManager _entity = default!;

    [DataField("divisor")]
    public int Divisor = 1;

    [DataField("offset")]
    public Vector2 Offset = new(0.5f, 0.5f);

    [DataField("initialOffset")]
    public Vector2 InitialOffset = Vector2.Zero;

    public override Type? SnappingModeConfigControl { get; } = null;



    public override EntityCoordinates Snap(EntityCoordinates coords)
    {
        return coords.WithPosition(((coords.Position + InitialOffset) * Divisor).Floored() / (float)Divisor + Offset);
    }

    public override void DrawSnapGuides(EntityCoordinates coords, in OverlayDrawArgs args)
    {
        return;
    }

    public override SnappingModeImpl Clone()
    {
        var @new = new AlignedSnappingMode();
        var ser = IoCManager.Resolve<ISerializationManager>();
        ser.Copy(this, ref @new);
        IoCManager.InjectDependencies(@new);
        return @new;
    }

    public override bool ValidateNewInitialPoint(EntityCoordinates old, EntityCoordinates @new)
    {
        if (@new.TryDistance(_entity, old, out var dist) && dist >= (1.0f/Divisor))
        {
            return true;
        }

        return false;
    }
}
