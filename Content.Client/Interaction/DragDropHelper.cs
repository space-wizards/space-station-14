using Content.Shared.CCVar;
using Robust.Client.Input;
using Robust.Shared.Configuration;
using Robust.Shared.Map;

namespace Content.Client.Interaction;

/// <summary>
/// Helper for implementing drag and drop interactions.
///
/// The basic flow for a drag drop interaction as per this helper is:
/// 1. User presses mouse down on something (using class should communicate this to helper by calling MouseDown()).
/// 2. User continues to hold the mouse down and moves the mouse outside of the defined
///    deadzone. OnBeginDrag is invoked to see if a drag should be initiated. If so, initiates a drag.
///    If user didn't move the mouse beyond the deadzone the drag is not initiated (OnEndDrag invoked).
/// 3. Every Update/FrameUpdate, OnContinueDrag is invoked.
/// 4. User lifts mouse up. This is not handled by DragDropHelper. The using class of the helper should
///     do whatever they want and then end the drag by calling EndDrag() (which invokes OnEndDrag).
///
/// If for any reason the drag is ended, OnEndDrag is invoked.
/// </summary>
/// <typeparam name="T">thing being dragged and dropped</typeparam>
public sealed class DragDropHelper<T>
{
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private readonly OnBeginDrag _onBeginDrag;
    private readonly OnEndDrag _onEndDrag;
    private readonly OnContinueDrag _onContinueDrag;
    private float _deadzone;

    /// <summary>
    /// Convenience method, current mouse screen position as provided by inputmanager.
    /// </summary>
    public ScreenCoordinates MouseScreenPosition => _inputManager.MouseScreenPosition;

    /// <summary>
    /// True if initiated a drag and currently dragging something.
    /// I.e. this will be false if we've just had a mousedown over something but the mouse
    /// has not moved outside of the drag deadzone.
    /// </summary>
    public bool IsDragging => _state == DragState.Dragging;

    /// <summary>
    /// Current thing being dragged or which mouse button is being held down on.
    /// </summary>
    public T? Dragged { get; private set; }

    // screen pos where the mouse down began for the drag
    private ScreenCoordinates _mouseDownScreenPos;
    private DragState _state = DragState.NotDragging;

    private enum DragState : byte
    {
        NotDragging,
        // not dragging yet, waiting to see
        // if they hold for long enough
        MouseDown,
        // currently dragging something
        Dragging,
    }

    /// <param name="onBeginDrag"><see cref="OnBeginDrag"/></param>
    /// <param name="onContinueDrag"><see cref="OnContinueDrag"/></param>
    /// <param name="onEndDrag"><see cref="OnEndDrag"/></param>
    public DragDropHelper(OnBeginDrag onBeginDrag, OnContinueDrag onContinueDrag, OnEndDrag onEndDrag)
    {
        IoCManager.InjectDependencies(this);
        _onBeginDrag = onBeginDrag;
        _onEndDrag = onEndDrag;
        _onContinueDrag = onContinueDrag;
        _cfg.OnValueChanged(CCVars.DragDropDeadZone, SetDeadZone, true);
    }

    ~DragDropHelper()
    {
        _cfg.UnsubValueChanged(CCVars.DragDropDeadZone, SetDeadZone);
    }

    /// <summary>
    /// Tell the helper that the mouse button was pressed down on
    /// a target, thus a drag has the possibility to begin for this target.
    /// Assumes current mouse screen position is the location the mouse was clicked.
    ///
    /// EndDrag should be called when the drag is done.
    /// </summary>
    public void MouseDown(T target)
    {
        if (_state != DragState.NotDragging)
        {
            EndDrag();
        }

        Dragged = target;
        _state = DragState.MouseDown;
        _mouseDownScreenPos = _inputManager.MouseScreenPosition;
    }

    /// <summary>
    /// Stop the current drag / drop operation no matter what state it is in.
    /// </summary>
    public void EndDrag()
    {
        Dragged = default;
        _state = DragState.NotDragging;
        _onEndDrag.Invoke();
    }

    private void StartDragging()
    {
        if (_onBeginDrag.Invoke())
        {
            _state = DragState.Dragging;
        }
        else
        {
            EndDrag();
        }
    }

    /// <summary>
    /// Should be invoked by using class every FrameUpdate or Update.
    /// </summary>
    public void Update(float frameTime)
    {
        switch (_state)
        {
            // check if dragging should begin
            case DragState.MouseDown:
            {
                var screenPos = _inputManager.MouseScreenPosition;
                if ((_mouseDownScreenPos.Position - screenPos.Position).Length() > _deadzone)
                {
                    StartDragging();
                }

                break;
            }
            case DragState.Dragging:
            {
                if (!_onContinueDrag.Invoke(frameTime))
                {
                    EndDrag();
                }

                break;
            }
        }
    }

    private void SetDeadZone(float value)
    {
        _deadzone = value;
    }
}

/// <summary>
/// Invoked when a drag is confirmed and going to be initiated. Implementation should
/// typically set the drag shadow texture based on the target.
/// </summary>
/// <returns>true if drag should begin, false to end.</returns>
public delegate bool OnBeginDrag();

/// <summary>
/// Invoked every frame when drag is ongoing. Typically implementation should
/// make the drag shadow follow the mouse position.
/// </summary>
/// <returns>true if drag should continue, false to end.</returns>
public delegate bool OnContinueDrag(float frameTime);

/// <summary>
/// invoked when
/// the drag drop is ending for any reason. This
/// should typically just clear the drag shadow.
/// </summary>
public delegate void OnEndDrag();
