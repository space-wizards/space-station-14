using System.Numerics;
using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;

namespace Content.Client.Weapons.Ranged.ItemStatus;

public abstract class BaseBulletRenderer : Control
{
    private int _capacity;
    private LayoutParameters _params;

    public int Rows { get; set; } = 2;
    public int Count { get; set; }

    public int Capacity
    {
        get => _capacity;
        set
        {
            if (_capacity == value)
                return;

            _capacity = value;
            InvalidateMeasure();
        }
    }

    protected LayoutParameters Parameters
    {
        get => _params;
        set
        {
            _params = value;
            InvalidateMeasure();
        }
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        var countPerRow = Math.Min(Capacity, CountPerRow(availableSize.X));

        var rows = Math.Min((int) MathF.Ceiling(Capacity / (float) countPerRow), Rows);

        var height = _params.ItemHeight * rows + (_params.VerticalSeparation * rows - 1);
        var width = RowWidth(countPerRow);

        return new Vector2(width, height);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        // Scale rendering in this control by UIScale.
        var currentTransform = handle.GetTransform();
        handle.SetTransform(Matrix3.CreateScale(new Vector2(UIScale)) * currentTransform);

        var countPerRow = CountPerRow(Size.X);

        var pos = new Vector2();

        var spent = Capacity - Count;

        var bulletsDone = 0;

        // Draw by rows, bottom to top.
        for (var row = 0; row < Rows; row++)
        {
            var altColor = false;

            var thisRowCount = Math.Min(countPerRow, Capacity - bulletsDone);
            if (thisRowCount <= 0)
                break;

            // Handle MinCountPerRow
            // We only do this if:
            // 1. The next row would have less than MinCountPerRow bullets.
            // 2. The next row is actually visible (we aren't the last row).
            // 3. MinCountPerRow is actually smaller than the count per row (avoid degenerate cases).
            // 4. There's enough bullets that at least one will end up on the next row.
            var nextRowCount = Capacity - bulletsDone - thisRowCount;
            if (nextRowCount < _params.MinCountPerRow && row != Rows - 1 && _params.MinCountPerRow < countPerRow && nextRowCount > 0)
                thisRowCount -= _params.MinCountPerRow - nextRowCount;

            // Account for row width to right-align.
            var rowWidth = RowWidth(thisRowCount);
            pos.X += Size.X - rowWidth;

            // Draw row left to right (so overlapping works)
            for (var bullet = 0; bullet < thisRowCount; bullet++)
            {
                var absIdx = Capacity - bulletsDone - thisRowCount + bullet;

                var renderPos = pos;
                renderPos.Y = Size.Y - renderPos.Y - _params.ItemHeight;

                DrawItem(handle, renderPos, absIdx < spent, altColor);

                pos.X += _params.ItemSeparation;
                altColor ^= true;
            }

            bulletsDone += thisRowCount;
            pos.X = 0;
            pos.Y += _params.ItemHeight + _params.VerticalSeparation;
        }
    }

    protected abstract void DrawItem(DrawingHandleScreen handle, Vector2 renderPos, bool spent, bool altColor);

    private int CountPerRow(float width)
    {
        return (int) ((width - _params.ItemWidth + _params.ItemSeparation) / _params.ItemSeparation);
    }

    private int RowWidth(int count)
    {
        return (count - 1) * _params.ItemSeparation + _params.ItemWidth;
    }

    protected struct LayoutParameters
    {
        public int ItemHeight;
        public int ItemSeparation;
        public int ItemWidth;
        public int VerticalSeparation;

        /// <summary>
        /// Try to ensure there's at least this many bullets on one row.
        /// </summary>
        /// <remarks>
        /// For example, if there are two rows and the second row has only two bullets,
        /// we "steal" some bullets from the row below it to make it look nicer.
        /// </remarks>
        public int MinCountPerRow;
    }
}

/// <summary>
/// Renders one or more rows of bullets for item status.
/// </summary>
/// <remarks>
/// This is a custom control to allow complex responsive layout logic.
/// </remarks>
public sealed class BulletRender : BaseBulletRenderer
{
    public const int MinCountPerRow = 7;

    public const int BulletHeight = 12;
    public const int VerticalSeparation = 2;

    private static readonly LayoutParameters LayoutNormal = new LayoutParameters
    {
        ItemHeight = BulletHeight,
        ItemSeparation = 3,
        ItemWidth = 5,
        VerticalSeparation = VerticalSeparation,
        MinCountPerRow = MinCountPerRow
    };

    private static readonly LayoutParameters LayoutTiny = new LayoutParameters
    {
        ItemHeight = BulletHeight,
        ItemSeparation = 2,
        ItemWidth = 2,
        VerticalSeparation = VerticalSeparation,
        MinCountPerRow = MinCountPerRow
    };

    private static readonly Color ColorA = Color.FromHex("#b68f0e");
    private static readonly Color ColorB = Color.FromHex("#d7df60");
    private static readonly Color ColorGoneA = Color.FromHex("#000000");
    private static readonly Color ColorGoneB = Color.FromHex("#222222");

    private readonly Texture _bulletTiny;
    private readonly Texture _bulletNormal;

    private BulletType _type = BulletType.Normal;

    public BulletType Type
    {
        get => _type;
        set
        {
            if (_type == value)
                return;

            Parameters = _type switch
            {
                BulletType.Normal => LayoutNormal,
                BulletType.Tiny => LayoutTiny,
                _ => throw new ArgumentOutOfRangeException()
            };

            _type = value;
        }
    }

    public BulletRender()
    {
        var resC = IoCManager.Resolve<IResourceCache>();
        _bulletTiny = resC.GetTexture("/Textures/Interface/ItemStatus/Bullets/tiny.png");
        _bulletNormal = resC.GetTexture("/Textures/Interface/ItemStatus/Bullets/normal.png");
        Parameters = LayoutNormal;
    }

    protected override void DrawItem(DrawingHandleScreen handle, Vector2 renderPos, bool spent, bool altColor)
    {
        Color color;
        if (spent)
            color = altColor ? ColorGoneA : ColorGoneB;
        else
            color = altColor ? ColorA : ColorB;

        var texture = _type == BulletType.Tiny ? _bulletTiny : _bulletNormal;
        handle.DrawTexture(texture, renderPos, color);
    }

    public enum BulletType
    {
        Normal,
        Tiny
    }
}

public sealed class BatteryBulletRenderer : BaseBulletRenderer
{
    private static readonly Color ItemColor = Color.FromHex("#E00000");
    private static readonly Color ItemColorGone = Color.Black;

    private const int SizeH = 10;
    private const int SizeV = 10;
    private const int Separation = 4;

    public BatteryBulletRenderer()
    {
        Parameters = new LayoutParameters
        {
            ItemWidth = SizeH,
            ItemHeight = SizeV,
            ItemSeparation = SizeH + Separation,
            MinCountPerRow = 3,
            VerticalSeparation = Separation
        };
    }

    protected override void DrawItem(DrawingHandleScreen handle, Vector2 renderPos, bool spent, bool altColor)
    {
        var color = spent ? ItemColorGone : ItemColor;
        handle.DrawRect(UIBox2.FromDimensions(renderPos, new Vector2(SizeH, SizeV)), color);
    }
}
