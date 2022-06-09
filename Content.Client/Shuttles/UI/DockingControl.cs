using Robust.Client.Graphics;
using Robust.Client.UserInterface;

namespace Content.Client.Shuttles.UI;

/// <summary>
/// Displays the docking view from a specific docking port
/// </summary>
[Virtual]
public class DockingControl : Control
{
    private IEntityManager _entManager;

    private const int MinimapRadius = 384;
    private const int MinimapMargin = 4;

    private int MidPoint => SizeFull / 2;
    private int SizeFull => (int) ((MinimapRadius + MinimapMargin) * 2 * UIScale);
    private int ScaledMinimapRadius => (int) (MinimapRadius * UIScale);

    private float Scale = 64f;

    public EntityUid? Entity;

    public DockingControl()
    {
        _entManager = IoCManager.Resolve<IEntityManager>();
        MinSize = (SizeFull, SizeFull);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var fakeAA = new Color(0.08f, 0.08f, 0.08f);

        handle.DrawCircle((MidPoint, MidPoint), ScaledMinimapRadius + 1, fakeAA);
        handle.DrawCircle((MidPoint, MidPoint), ScaledMinimapRadius, Color.Black);

        if (!_entManager.TryGetComponent<TransformComponent>(Entity, out var xform)) return;

        var matrix = xform.InvWorldMatrix;
        var rotation = Matrix3.CreateRotation(xform.LocalRotation);

        // Draw the dock's collision
        handle.DrawRect(new UIBox2(
            ScalePosition(rotation.Transform(new Vector2(0.2f, -0.2f))),
            ScalePosition(rotation.Transform(new Vector2(0.2f, 0f)))), Color.Aquamarine, false);

        // Draw the dock itself
        handle.DrawLine(ScalePosition(rotation.Transform(new Vector2(-0.5f, 0f))), ScalePosition(rotation.Transform(new Vector2(0.5f, 0f))), Color.Orange);

        // Draw the fixtures around it
    }

    private Vector2 ScalePosition(Vector2 value)
    {
        return value * Scale + MidPoint;
    }
}
