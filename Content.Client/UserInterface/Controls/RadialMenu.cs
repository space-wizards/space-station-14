using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using System.Linq;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;

namespace Content.Client.UserInterface.Controls;

[Virtual]
public class RadialMenu : BaseWindow
{
    /// <summary>
    /// Contextual button used to traverse through previous layers of the radial menu
    /// </summary>
    public TextureButton? ContextualButton { get; set; }

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
        ContextualButton = new TextureButton()
        {
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            SetSize = new Vector2(64f, 64f),
        };

        ContextualButton.OnButtonUp += _ => ReturnToPreviousLayer();
        AddChild(ContextualButton);

        // Hide any further add children, unless its promoted to the active layer
        OnChildAdded += child => child.Visible = GetCurrentActiveLayer() == child;
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

[Virtual]
public class RadialMenuButton : Button
{
    /// <summary>
    /// Upon clicking this button the radial menu will transition to the named layer
    /// </summary>
    public string? TargetLayer { get; set; }

    /// <summary>
    /// A simple button that can move the user to a different layer within a radial menu
    /// </summary>
    public RadialMenuButton()
    {
        OnButtonUp += OnClicked;
    }

    private void OnClicked(ButtonEventArgs args)
    {
        if (string.IsNullOrEmpty(TargetLayer))
            return;

        var parent = FindParentMultiLayerContainer(this);

        if (parent == null)
            return;

        parent.TryToMoveToNewLayer(TargetLayer);
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
}

[Virtual]
public class RadialMenuTextureButton : TextureButton
{
    /// <summary>
    /// Upon clicking this button the radial menu will be moved to the named layer
    /// </summary>
    public string TargetLayer { get; set; } = string.Empty;

    public float AngleSectorFrom { get; set; }
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

        handle.DrawTextureRectRegion(texture, PixelSizeBox);

        var pX = -Position.X + Parent!.Width / 2;
        var pY = -Position.Y + Parent.Width / 2;

        var position = new Vector2(pX, pY) * UIScale;

        var singleSegmentSize = MathF.Tau / 32;

        var controlSegmentSize = AngleSectorTo - AngleSectorFrom;
        var segCount = (int) (controlSegmentSize / singleSegmentSize) +1;

        // CHANGE TO STACKALLOC AND MOVE THIS STUFF TO TOOLBOX!111
        var buffer = new Vector2[segCount * 2];

        var radius = (Parent as RadialContainer)!.Radius * UIScale;

        for (var i = 0; i < segCount; i++)
        {
            float angle;
            if (i == segCount - 1)
            {
                angle = AngleSectorTo;
            }
            else
            {
                angle = AngleSectorFrom + singleSegmentSize * i;
            }

            var pos = new Angle(angle).RotateVec(-Vector2.UnitY);

            buffer[i * 2] = position + pos * radius * 2;
            buffer[i * 2 + 1] = position + pos * radius / 2;
        }

        var color = DrawMode == DrawModeEnum.Hover
            ? new Color(173, 216, 230, 100)
            : new Color(173, 216, 230, 70);
        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleStrip, buffer, color);
    }

    /// <inheritdoc />
    protected override void MouseEntered()
    {
        //Console.WriteLine(this._texturePath?.ToString());
        base.MouseEntered();
    }

    /// <inheritdoc />
    protected override void MouseExited()
    {
        //Console.WriteLine(this._texturePath?.ToString());
        base.MouseExited();
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
    public override bool IsPositionInside(Vector2i point)
    {
        var cX = -Position.X + Parent!.Width / 2;
        var cY = -Position.Y + Parent.Width / 2;

        var dist = GetDistance(cX, cY, point.X, point.Y);

        var dX = cX - point.X;
        var dY = cY - point.Y;
        var angle = dX > 0 ? -MathF.Atan2(dX, dY) + MathF.PI * 2 : -MathF.Atan2(dX, dY);
        var parent = (RadialContainer)Parent;
        var isInAngle = angle > AngleSectorFrom && angle < AngleSectorTo;
        var isInRadius = dist < parent.Radius * 2 && dist > parent.Radius / 2;
        var hasPoint = isInRadius && isInAngle;
        return hasPoint;
    }

    /// <inheritdoc />
    protected override bool HasPoint(Vector2 point)
    {
        var cX = -Position.X + Parent!.Width / 2;
        var cY = -Position.Y + Parent.Width / 2;

        var dist = GetDistance(cX, cY, point.X, point.Y);

        var dX = cX - point.X;
        var dY = cY - point.Y;
        var angle = dX > 0 ? -MathF.Atan2(dX,dY) + MathF.PI *2: - MathF.Atan2(dX, dY);
        var parent = (RadialContainer)Parent;
        var isInAngle = angle > AngleSectorFrom && angle < AngleSectorTo;
        var isInRadius = dist < parent.Radius*2 && dist > parent.Radius/2;
        var hasPoint = isInRadius && isInAngle;
        return hasPoint;
    }

    public static float ConvertRadiansToDegrees(float radians)
    {
        float degrees = (180f / MathF.PI) * radians;
        return (degrees);
    }


    private static double GetDistance(float x1, float y1, float x2, float y2)
    {
        return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));

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
}
