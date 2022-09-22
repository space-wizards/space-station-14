using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls;

/// <summary>
///     Simple control that shows an arrow pointing in some direction.
/// </summary>
/// <remarks>
///     The actual arrow and other icons are defined in the style sheet.
/// </remarks>
public sealed class DirectionIcon : TextureRect
{
    public static string StyleClassDirectionIconArrow = "direction-icon-arrow"; // south pointing arrow
    public static string StyleClassDirectionIconHere = "direction-icon-here"; // "you have reached your destination"
    public static string StyleClassDirectionIconUnknown = "direction-icon-unknown"; // unknown direction / error

    private Angle? _rotation;

    public Angle? Rotation
    {
        get => _rotation;
        set
        {
            _rotation = value;
            SetOnlyStyleClass(value == null ? StyleClassDirectionIconUnknown : StyleClassDirectionIconArrow);
        }
    }

    public DirectionIcon()
    {
        Stretch = StretchMode.KeepAspectCentered;
        SetOnlyStyleClass(StyleClassDirectionIconUnknown);
    }

    public DirectionIcon(Direction direction) : this()
    {
        Rotation = direction.ToAngle();
    }

    /// <summary>
    ///     Creates an icon with an arrow pointing in some direction.
    /// </summary>
    /// <param name="direction">The direction</param>
    /// <param name="relativeAngle">The relative angle. This may be the players eye rotation, the grid rotation, or
    /// maybe the world rotation of the entity that owns some BUI</param>
    /// <param name="snap">If true, will snap the nearest cardinal or diagonal direction</param>
    /// <param name="minDistance">If the distance is less than this, the arrow icon will be replaced by some other indicator</param>
    public DirectionIcon(Vector2 direction, Angle relativeAngle, bool snap, float minDistance = 0.1f) : this()
    {
        if (direction.EqualsApprox(Vector2.Zero, minDistance))
        {
            SetOnlyStyleClass(StyleClassDirectionIconHere);
            return;
        }

        var rotation = direction.ToWorldAngle() - relativeAngle;
        Rotation = snap ? rotation.GetDir().ToAngle()  : rotation;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (_rotation != null)
        {
            var offset = (-_rotation.Value).RotateVec(Size * UIScale / 2) - Size * UIScale / 2;
            handle.SetTransform(Matrix3.CreateTransform(GlobalPixelPosition - offset, -_rotation.Value));
        }

        base.Draw(handle);
    }
}
