using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using System.Linq;
using System.Numerics;
using Robust.Client.Graphics;

namespace Content.Client.UserInterface.Controls;

[Virtual]
public class RadialMenu : BaseWindow
{
    /// <summary>
    /// Contextual button used to traverse through previous layers of the radial menu
    /// </summary>
    public RadialMenuContextualCentralTextureButton ContextualButton { get; }

    /// <summary>
    /// Set a style class to be applied to the contextual button when it is set to move the user back through previous layers of the radial menu
    /// </summary>  
    public string? BackButtonStyleClass
    {
        get
        {
            return _backButtonStyleClass;
        }

        set
        {
            _backButtonStyleClass = value;

            if (_path.Count > 0 && ContextualButton != null && _backButtonStyleClass != null)
                ContextualButton.SetOnlyStyleClass(_backButtonStyleClass);
        }
    }

    /// <summary>
    /// Set a style class to be applied to the contextual button when it will close the radial menu
    /// </summary>
    public string? CloseButtonStyleClass
    {
        get
        {
            return _closeButtonStyleClass;
        }

        set
        {
            _closeButtonStyleClass = value;

            if (_path.Count == 0 && ContextualButton != null && _closeButtonStyleClass != null)
                ContextualButton.SetOnlyStyleClass(_closeButtonStyleClass);
        }
    }

    private readonly List<Control> _path = new();
    private string? _backButtonStyleClass;
    private string? _closeButtonStyleClass;

    /// <summary>
    /// A free floating menu which enables the quick display of one or more radial containers
    /// </summary>
    /// <remarks>
    /// Only one radial container is visible at a time (each container forming a separate 'layer' within 
    /// the menu), along with a contextual button at the menu center, which will either return the user  
    /// to the previous layer or close the menu if there are no previous layers left to traverse.
    /// To create a functional radial menu, simply parent one or more named radial containers to it,
    /// and populate the radial containers with RadialMenuButtons. Setting the TargetLayer field of these
    /// buttons to the name of a radial conatiner will display the container in question to the user
    /// whenever it is clicked in additon to any other actions assigned to the button
    /// </remarks>
    public RadialMenu()
    {
        // Hide all starting children (if any) except the first (this is the active layer)
        if (ChildCount > 1)
        {
            for (int i = 1; i < ChildCount; i++)
                GetChild(i).Visible = false;
        }

        // Auto generate a contextual button for moving back through visited layers
        ContextualButton = new RadialMenuContextualCentralTextureButton
        {
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            SetSize = new Vector2(64f, 64f),
        };

        ContextualButton.OnButtonUp += _ => ReturnToPreviousLayer();
        AddChild(ContextualButton);

        // Hide any further add children, unless its promoted to the active layer
        OnChildAdded += child =>
        {
            child.Visible = GetCurrentActiveLayer() == child;
            if (child is RadialContainer { Visible: true } container)
                ContextualButton.ActiveContainer = container;
        };
    }

    private Control? GetCurrentActiveLayer()
    {
        var children = Children.Where(x => x != ContextualButton);

        if (!children.Any())
            return null;

        return children.First(x => x.Visible);
    }

    public bool TryToMoveToNewLayer(string newLayer)
    {
        if (newLayer == string.Empty)
            return false;

        var currentLayer = GetCurrentActiveLayer();

        if (currentLayer == null)
            return false;

        var result = false;

        foreach (var child in Children)
        {
            if (child == ContextualButton)
                continue;

            // Hide layers which are not of interest
            if (result == true || child.Name != newLayer)
            {
                child.Visible = false;
            }

            // Show the layer of interest
            else
            {
                child.Visible = true;
                if (child is RadialContainer container)
                    ContextualButton.ActiveContainer = container;
                result = true;
            }
        }

        // Update the traversal path
        if (result)
            _path.Add(currentLayer);

        // Set the style class of the button
        if (_path.Count > 0 && ContextualButton != null && BackButtonStyleClass != null)
            ContextualButton.SetOnlyStyleClass(BackButtonStyleClass);

        return result;
    }

    public void ReturnToPreviousLayer()
    {
        // Close the menu if the traversal path is empty
        if (_path.Count == 0)
        {
            Close();
            return;
        }

        var lastChild = _path[^1];

        // Hide all children except the contextual button
        foreach (var child in Children)
        {
            if (child != ContextualButton)
                child.Visible = false;
        }

        // Make the last visited layer visible, update the path list
        lastChild.Visible = true;
        _path.RemoveAt(_path.Count - 1);

        // Set the style class of the button
        if (_path.Count == 0 && ContextualButton != null && CloseButtonStyleClass != null)
            ContextualButton.SetOnlyStyleClass(CloseButtonStyleClass);
    }
}

/// <summary>
/// Special button for closing radial menu or going back between radial menu levels.
/// Is looking like just <see cref="TextureButton "/> but considers whole space around
/// itself (til radial menu buttons) as itself in case of clicking. Also considers all space
/// outside of radial menu buttons as itself for clicking.
/// </summary>
public sealed class RadialMenuContextualCentralTextureButton : TextureButton
{

    /// <inheritdoc />
    public RadialMenuContextualCentralTextureButton()
    {

    }

    /// <summary>
    /// Reference for container of radial menu.
    /// Is required to properly consider radius of circles menu will draw.
    /// </summary>
    public RadialContainer? ActiveContainer { get; set; }

    /// <inheritdoc />
    protected override bool HasPoint(Vector2 point)
    {
        if (ActiveContainer == null)
        {
            return false;
        }

        var cX = -Position.X + Parent!.Width / 2;
        var cY = -Position.Y + Parent.Width / 2;

        var dist = Math.Sqrt(Math.Pow(point.X - cX, 2) + Math.Pow(point.Y - cY, 2));
        // Button space is inside half of container radius / or outside double of its radius.
        // half of radius and double the radius are radial menu concentric circles that are
        // created by radial menu buttons.
        return dist > ActiveContainer.Radius * 2 || dist < ActiveContainer.Radius / 2;
    }
}

[Virtual]
public class RadialMenuTextureButton : TextureButton
{
    private Vector2[]? _sectorPointsForDrawing;

    /// <summary>
    /// Upon clicking this button the radial menu will be moved to the named layer
    /// </summary>
    public string TargetLayer { get; set; } = string.Empty;

    /// <summary>
    /// Angle in radian where button sector should start.
    /// </summary>
    public float AngleSectorFrom { get; set; }

    /// <summary>
    /// Angle in radian where button sector should end.
    /// </summary>
    public float AngleSectorTo { get; set; }

    /// <summary>
    /// A simple texture button that can move the user to a different layer within a radial menu
    /// </summary>
    public RadialMenuTextureButton()
    {
        OnButtonUp += OnClicked;
    }

    /// <inheritdoc />
    protected override void Draw(DrawingHandleScreen handle)
    {
        var texture = TextureNormal;

        if (texture == null)
        {
            TryGetStyleProperty(StylePropertyTexture, out texture);
            if (texture == null)
            {
                return;
            }
        }
        // draw texture
        handle.DrawTextureRectRegion(texture, PixelSizeBox);

        // draw sector where space that button occupies actually is
        var pX = -Position.X + Parent!.Width / 2;
        var pY = -Position.Y + Parent.Width / 2;

        var containerCenter = new Vector2(pX, pY) * UIScale;

        if (Parent is not RadialContainer container)
        {
            return;
        }

        var radius = container.Radius * UIScale;

        var color = DrawMode == DrawModeEnum.Hover
            ? new Color(173, 216, 230, 100)
            : new Color(173, 216, 230, 70); // todo: use stylesheets

        DrawBagleSector(handle, containerCenter, radius / 2, radius * 2, AngleSectorFrom, AngleSectorTo, color);
    }

    private void OnClicked(ButtonEventArgs args)
    {
        if (TargetLayer == string.Empty)
            return;

        var parent = FindParentMultiLayerContainer(this);

        if (parent == null)
            return;

        parent.TryToMoveToNewLayer(TargetLayer);
    }

    /// <inheritdoc />
    protected override bool HasPoint(Vector2 point)
    {
        var cX = -Position.X + Parent!.Width / 2;
        var cY = -Position.Y + Parent.Width / 2;

        var dist = Math.Sqrt(Math.Pow(point.X - cX, 2) + Math.Pow(point.Y - cY, 2));
        var parent = (RadialContainer)Parent;
        var isInRadius = dist < parent.Radius * 2 && dist > parent.Radius / 2;
        if (!isInRadius)
        {
            return false;
        }

        var dX = cX - point.X;
        var dY = cY - point.Y;
        var angle = dX > 0
            ? -MathF.Atan2(dX, dY) + MathF.PI * 2
            : -MathF.Atan2(dX, dY);
        var isInAngle = angle > AngleSectorFrom && angle < AngleSectorTo;
        return isInAngle;
    }

    private RadialMenu? FindParentMultiLayerContainer(Control control)
    {
        foreach (var ancestor in control.GetSelfAndLogicalAncestors())
        {
            if (ancestor is RadialMenu menu)
                return menu;
        }

        return null;
    }

    /// <summary>
    /// Draw segment between two concentrated circles from and to certain angles.
    /// </summary>
    /// <param name="drawingHandleScreen">Drawing handle, to which rendering should be delegated.</param>
    /// <param name="center">Point where circle center should be.</param>
    /// <param name="radiusInner">Radius of internal circle.</param>
    /// <param name="radiusOuter">Radius of external circle.</param>
    /// <param name="angleSectorFrom">Angle in radian, from which sector should start.</param>
    /// <param name="angleSectorTo">Angle in radian, from which sector should start.</param>
    /// <param name="color">Color for drawing.</param>
    /// <param name="filled">Should figure be filled, or have only border.</param>
    private void DrawBagleSector(
        DrawingHandleScreen drawingHandleScreen,
        Vector2 center,
        float radiusInner,
        float radiusOuter,
        float angleSectorFrom,
        float angleSectorTo,
        Color color,
        bool filled = true
    )
    {
        const float minimalSegmentSize = MathF.Tau / 32;

        var requestedSegmentSize = angleSectorTo - angleSectorFrom;
        var segmentCount = (int)(requestedSegmentSize / minimalSegmentSize) + 1;

        var bufferSize = segmentCount * 2;
        if (_sectorPointsForDrawing == null || _sectorPointsForDrawing.Length != bufferSize)
        {
            _sectorPointsForDrawing ??= new Vector2[bufferSize];
        }

        for (var i = 0; i < segmentCount; i++)
        {
            float angle;
            if (i == segmentCount - 1)
            {
                // fix rounding problem that was created when calculating count of segments as int
                angle = angleSectorTo;
            }
            else
            {
                angle = angleSectorFrom + minimalSegmentSize * i;
            }

            var point = new Angle(angle).RotateVec(-Vector2.UnitY);
            var outerPoint = center + point * radiusOuter;
            var innerPoint = center + point * radiusInner;
            if (filled)
            {
                // to make filled sector we need to create strip from triangles
                _sectorPointsForDrawing[i * 2] = outerPoint;
                _sectorPointsForDrawing[i * 2 + 1] = innerPoint;
            }
            else
            {
                // to make border of sector we need points ordered as sequences on radius
                _sectorPointsForDrawing[i] = outerPoint;
                _sectorPointsForDrawing[bufferSize - 1 - i] = innerPoint;
            }

        }

        var type = filled
            ? DrawPrimitiveTopology.TriangleStrip
            : DrawPrimitiveTopology.LineStrip;
        drawingHandleScreen.DrawPrimitives(type, _sectorPointsForDrawing, color);
    }
}
