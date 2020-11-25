using System;
using Robust.Client.Interfaces.Input;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.Utility
{
    /// <summary>
    /// Helper for dealing with drag and drop interactions.
    /// </summary>
    /// <typeparam name="T">thing being dragged and dropped</typeparam>
    public class DragDropHelper<T>
    {
        private readonly IInputManager _inputManager;

        private readonly OnBeginDrag _onBeginDrag;
        private readonly OnEndDrag _onEndDrag;
        private readonly OnContinueDrag _onContinueDrag;
        private readonly float _deadzone;

        /// <summary>
        /// Convenience method, current mouse screen position as provided by inputmanager.
        /// </summary>
        public Vector2 MouseScreenPos => _inputManager.MouseScreenPosition;

        /// <summary>
        /// True if currently dragging something.
        /// </summary>
        public bool IsDragging => _state == DragState.Dragging;

        /// <summary>
        /// Current thing being dragged or which mouse button is being held down on.
        /// </summary>
        public T Target { get; private set; }

        // screen pos where the mouse down began for the drag
        private Vector2 _mouseDownScreenPos;
        private DragState _state = DragState.NotDragging;

        private enum DragState
        {
            NotDragging,
            // not dragging yet, waiting to see
            // if they hold for long enough
            MouseDown,
            // currently dragging something
            Dragging,
        }

        /// <param name="deadzone">drag will be triggered when mouse leaves
        ///     this deadzone around the mousedown position</param>
        /// <param name="onBeginDrag"><see cref="OnBeginDrag"/></param>
        /// <param name="onContinueDrag"><see cref="OnContinueDrag"/></param>
        /// <param name="onEndDrag"><see cref="OnEndDrag"/></param>
        public DragDropHelper(float deadzone, OnBeginDrag onBeginDrag, OnContinueDrag onContinueDrag,
            OnEndDrag onEndDrag)
        {
            _deadzone = deadzone;
            _inputManager = IoCManager.Resolve<IInputManager>();
            _onBeginDrag = onBeginDrag;
            _onEndDrag = onEndDrag;
            _onContinueDrag = onContinueDrag;
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
            EndDrag();

            Target = target;
            _state = DragState.MouseDown;
            _mouseDownScreenPos = _inputManager.MouseScreenPosition;
        }

        /// <summary>
        /// Stop the current drag / drop operation no matter what state it is in.
        /// </summary>
        public void EndDrag()
        {
            Target = default;
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
        /// Should be invoked by using class every FrameUpdate.
        /// </summary>
        /// <param name="args"></param>
        public void FrameUpdate()
        {
            switch (_state)
            {
                // check if dragging should begin
                case DragState.MouseDown:
                {
                    var screenPos = _inputManager.MouseScreenPosition;
                    if ((_mouseDownScreenPos - screenPos).Length > _deadzone)
                    {
                        StartDragging();
                    }

                    break;
                }
                case DragState.Dragging:
                {
                    if (!_onContinueDrag.Invoke())
                    {
                        EndDrag();
                    }

                    break;
                }
            }
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
    public delegate bool OnContinueDrag();

    /// <summary>
    /// invoked when
    /// the drag drop is ending for any reason. This
    /// should typically just clear the drag shadow.
    /// </summary>
    public delegate void OnEndDrag();

}
