using System.Numerics;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Lobby.UI;

/// <summary>
/// This class defines a UI control for a draggable job icon. These elements are to be used with
/// <see cref="DraggableJobTarget"/>
/// </summary>
public sealed class DraggableJobIcon : TextureRect
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    private const float DefaultScale = 3;
    private const float DefaultHighScale = 8;

    /// <summary>
    /// The job prototype represented by this icon
    /// </summary>
    public JobPrototype JobProto { get; private init; }

    /// <summary>
    /// If the icon is being dragged, this will hold a reference to the control that contained it before the dragging.
    /// If the drag has ended and no other elements reparented this icon, it will snap back to _oldParent.
    /// </summary>
    private Control? _oldParent;

    /// <summary>
    /// Helper to check if this icon is being dragged. The icon is being dragged if and only if _oldParent isn't null.
    /// </summary>
    public bool Dragging => _oldParent is not null;

    /// <summary>
    /// Event invoked when the icon has been pressed with UIClick
    /// </summary>
    public event Action<GUIBoundKeyEventArgs>? OnMouseDown;

    /// <summary>
    /// Event invoked when the icon has been released with UIClick
    /// </summary>
    public event Action<Vector2>? OnMouseUp;

    /// <summary>
    /// Event invoked when the mouse has been moved while the icon is being dragged
    /// </summary>
    public event Action<Vector2>? OnMouseMove;

    public DraggableJobIcon(JobPrototype jobPrototype, TooltipSupplier? tooltipSupplier = null)
    {
        IoCManager.InjectDependencies(this);

        JobProto = jobPrototype;

        var sprite = _entManager.System<SpriteSystem>();
        var iconProto = _prototypeManager.Index(jobPrototype.Icon);

        Texture = sprite.Frame0(iconProto.Icon);
        TextureScale = new Vector2(DefaultScale);
        VerticalAlignment = VAlignment.Center;
        HorizontalAlignment = HAlignment.Center;
        MouseFilter = MouseFilterMode.Pass;

        // Add a little sugar to suppress the tooltip while dragging the icon
        if(tooltipSupplier is not null)
            TooltipSupplier = obj => Dragging ? null : tooltipSupplier(obj);
    }

    public void StopDragging()
    {
        if (Parent == _uiManager.PopupRoot)
        {
            Orphan();
            _oldParent?.AddChild(this);
        }
        _oldParent = null;
    }

    private void StartDragging(GUIBoundKeyEventArgs args)
    {
        _oldParent = Parent;
        Orphan();
        _uiManager.PopupRoot.AddChild(this);

    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!Dragging)
            return;

        var mousePos = _uiManager.MousePositionScaled.Position;
        LayoutContainer.SetPosition(this, mousePos - Size / 2.0f);
        OnMouseMove?.Invoke(mousePos);
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);
        if (args.Function == EngineKeyFunctions.UIClick)
        {
            StartDragging(args);
            OnMouseDown?.Invoke(args);
        }
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);
        if (args.Function == EngineKeyFunctions.UIClick)
        {
            OnMouseUp?.Invoke(_uiManager.MousePositionScaled.Position);
            StopDragging();
        }
    }

    public void SetScale(JobPriority priority)
    {
        SetScale(priority == JobPriority.High ? DefaultHighScale : DefaultScale);
    }

    public void SetScale(float scale)
    {
        TextureScale = new Vector2(scale);
    }
}
