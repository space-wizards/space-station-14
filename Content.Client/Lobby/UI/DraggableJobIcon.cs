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

    /// <summary>
    /// The TextureScale of a job icon
    /// </summary>
    private const float DefaultScale = 3;

    /// <summary>
    /// The TextureScale of a job icon if it's in the high priority bucket
    /// </summary>
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
    /// If the icon is being dragged, this will hold the <see cref="TextureRect.TextureScale"/> that was set before the
    /// dragging. If the drag has ended and no other elements reparented this icon, it will reset the
    /// <see cref="TextureRect.TextureScale"/> back to this value..
    /// </summary>
    private Vector2? _oldScale;

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

    /// <summary>
    /// Event invoked after the icon stopped dragging and it ended up parented to a different object
    /// </summary>
    public event Action? OnPriorityChanged;

    /// <summary>
    /// Invoked when a drag is about to start, will be canceled if this returns false.
    /// </summary>
    public delegate bool CheckCanDrag();

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
        TooltipDelay = 0;

        // Add a little sugar to suppress the tooltip while dragging the icon
        if(tooltipSupplier is not null)
            TooltipSupplier = obj => Dragging ? null : tooltipSupplier(obj);
    }

    /// <summary>
    /// Called after all <see cref="OnMouseUp"/> events are called to clean up the drag event
    /// </summary>
    private void StopDragging()
    {
        // If nothing reparented the icon from the OnMouseUp events, then we should snap the icon back to its original
        // parent
        if (Parent == _uiManager.PopupRoot)
        {
            // Put it back and make sure the TextureScale is reset
            Orphan();
            _oldParent?.AddChild(this);
            if (_oldScale is not null)
                TextureScale = _oldScale.Value;
        }

        // If the parent changed, then invoke the event that priorities changed!
        if (Parent != _oldParent)
            OnPriorityChanged?.Invoke();

        _oldParent = null;
        _oldScale = null;
    }

    /// <summary>
    /// Start the process of dragging the icon
    /// </summary>
    private void StartDragging(GUIBoundKeyEventArgs args)
    {
        // Save the current parent and texture scale
        _oldParent = Parent;
        _oldScale = TextureScale;
        // Put it into PopupRoot
        Orphan();
        _uiManager.PopupRoot.AddChild(this);

    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        // We only need to do stuff if we're dragging
        if (!Dragging)
            return;

        // Track the icon to the mouse cursor
        var mousePos = _uiManager.MousePositionScaled.Position;
        LayoutContainer.SetPosition(this, mousePos - Size / 2.0f);
        // Let the drop targets check if you're dragging over them
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
            // Invoke this to let drop targets handle the icon
            OnMouseUp?.Invoke(_uiManager.MousePositionScaled.Position);
            // Clean up
            StopDragging();
        }
    }

    /// <summary>
    /// Set the TextureScale of the icon according to the job priority using constants
    /// </summary>
    public void SetScale(JobPriority priority)
    {
        SetScale(priority == JobPriority.High ? DefaultHighScale : DefaultScale);
    }

    /// <summary>
    /// Just a wrapper to set the TextureScale from a single float
    /// </summary>
    private void SetScale(float scale)
    {
        TextureScale = new Vector2(scale);
    }
}
