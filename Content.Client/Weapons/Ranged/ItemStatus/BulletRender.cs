using System.Numerics;
using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;

namespace Content.Client.Weapons.Ranged.ItemStatus;

/// <summary>
/// Renders one or more rows of bullets for item status.
/// </summary>
/// <remarks>
/// This is a custom control to allow complex responsive layout logic.
/// </remarks>
public sealed class BulletRender : Control
{
    private static readonly Color ColorA = Color.FromHex("#b68f0e");
    private static readonly Color ColorB = Color.FromHex("#d7df60");
    private static readonly Color ColorGoneA = Color.FromHex("#000000");
    private static readonly Color ColorGoneB = Color.FromHex("#222222");

    public const int BulletHeight = 12;
    public const int BulletSeparationNormal = 3;
    public const int BulletSeparationTiny = 2;
    public const int BulletWidthNormal = 5;
    public const int BulletWidthTiny = 2;
    public const int VerticalSeparation = 2;

    private readonly Texture _bulletTiny;
    private readonly Texture _bulletNormal;

    private int _capacity;
    private BulletType _type = BulletType.Normal;

    public int Rows { get; set; } = 2;
    public int Count { get; set; }

    public int Capacity
    {
        get => _capacity;
        set
        {
            _capacity = value;
            InvalidateMeasure();
        }
    }

    public BulletType Type
    {
        get => _type;
        set
        {
            _type = value;
            InvalidateMeasure();
        }
    }

    public BulletRender()
    {
        var resC = IoCManager.Resolve<IResourceCache>();
        _bulletTiny = resC.GetTexture("/Textures/Interface/ItemStatus/Bullets/tiny.png");
        _bulletNormal = resC.GetTexture("/Textures/Interface/ItemStatus/Bullets/normal.png");
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        var countPerRow = Math.Min(Capacity, CountPerRow(availableSize.X));

        var rows = Math.Min((int) MathF.Ceiling(Capacity / (float) countPerRow), Rows);

        var height = BulletHeight * rows + (BulletSeparationNormal * rows - 1);
        var width = RowWidth(countPerRow);

        return new Vector2(width, height);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        // Scale rendering in this control by UIScale.
        var currentTransform = handle.GetTransform();
        handle.SetTransform(Matrix3.CreateScale(new Vector2(UIScale)) * currentTransform);

        var countPerRow = CountPerRow(Size.X);

        var (separation, _) = BulletParams();
        var texture = Type == BulletType.Normal ? _bulletNormal : _bulletTiny;

        var pos = new Vector2();

        var altColor = false;

        var spent = Capacity - Count;

        var bulletsDone = 0;

        // Draw by rows, bottom to top.
        for (var row = 0; row < Rows; row++)
        {
            altColor = false;

            var thisRowCount = Math.Min(countPerRow, Capacity - bulletsDone);
            if (thisRowCount <= 0)
                break;

            // Account for row width to right-align.
            var rowWidth = RowWidth(thisRowCount);
            pos.X += Size.X - rowWidth;

            // Draw row left to right (so overlapping works)
            for (var bullet = 0; bullet < thisRowCount; bullet++)
            {
                var absIdx = Capacity - bulletsDone - thisRowCount + bullet;
                Color color;
                if (absIdx >= spent)
                    color = altColor ? ColorA : ColorB;
                else
                    color = altColor ? ColorGoneA : ColorGoneB;

                var renderPos = pos;
                renderPos.Y = Size.Y - renderPos.Y - BulletHeight;
                handle.DrawTexture(texture, renderPos, color);
                pos.X += separation;
                altColor ^= true;
            }

            bulletsDone += thisRowCount;
            pos.X = 0;
            pos.Y += BulletHeight + VerticalSeparation;
        }
    }

    private int CountPerRow(float width)
    {
        var (separation, bulletWidth) = BulletParams();
        return (int) ((width - bulletWidth + separation) / separation);
    }

    private (int separation, int width) BulletParams()
    {
        return Type switch
        {
            BulletType.Normal => (BulletSeparationNormal, BulletWidthNormal),
            BulletType.Tiny => (BulletSeparationTiny, BulletWidthTiny),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private int RowWidth(int count)
    {
        var (separation, bulletWidth) = BulletParams();

        return (count - 1) * separation + bulletWidth;
    }

    public enum BulletType
    {
        Normal,
        Tiny
    }
}
