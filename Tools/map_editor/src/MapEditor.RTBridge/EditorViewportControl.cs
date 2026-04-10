using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.ContentPack;
using Robust.Shared.Graphics;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace MapEditor.RTBridge;

/// <summary>
///     <see cref="ViewportContainer"/> subclass that hosts the editor's
///     viewport and handles camera controls by delegating to an
///     <see cref="EditorCamera"/>. This class is the thin adapter that
///     receives RT input events, translates them to camera operations, and
///     writes the resulting state back to the shared editor eye.
/// </summary>
/// <remarks>
///     Derives from <c>ViewportContainer</c> rather than
///     <c>MainViewportContainer</c> because the latter is sealed. We
///     replicate the one piece of behavior <c>MainViewportContainer</c>
///     adds: keeping the viewport's rendered eye in sync with
///     <see cref="IEyeManager.CurrentEye"/>.
///
///     <para>
///     All camera math lives in <see cref="EditorCamera"/> which is plain
///     C# with no RT dependencies, so it can be unit tested without
///     spinning up a full RT instance.
///     </para>
/// </remarks>
[ContentAccessAllowed]
public sealed class EditorViewportControl : ViewportContainer
{
    private readonly Eye _editorEye;
    private readonly IEyeManager _eyeManager;
    private readonly EditorCamera _camera;

    public EditorViewportControl(IEyeManager eyeManager, Eye editorEye)
    {
        _eyeManager = eyeManager;
        _editorEye = editorEye;
        _camera = new EditorCamera(
            position: editorEye.Position.Position,
            zoom: editorEye.Zoom);
        MouseFilter = MouseFilterMode.Stop;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        // Write any camera state changes back to the editor eye each
        // frame, and mirror MainViewportContainer's "always point at the
        // current eye" behavior since we cannot subclass it (sealed).
        SyncCameraToEye();
        if (Viewport != null)
            Viewport.Eye = _eyeManager.CurrentEye;
    }

    private void SyncCameraToEye()
    {
        _editorEye.Position = new MapCoordinates(_camera.Position, _editorEye.Position.MapId);
        _editorEye.Zoom = _camera.Zoom;
    }

    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        base.MouseWheel(args);
        if (args.Delta.Y == 0f)
            return;

        _camera.ApplyZoomStep(args.Delta.Y, args.RelativePosition, Size);
        args.Handle();
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function == EngineKeyFunctions.UseSecondary)
        {
            // Right click drag starts a pan.
            _camera.BeginPan(args.RelativePosition, Size);
            args.Handle();
        }
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function == EngineKeyFunctions.UseSecondary)
        {
            _camera.EndPan();
            args.Handle();
        }
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);
        if (_camera.IsPanning)
        {
            _camera.UpdatePan(args.RelativePosition, Size);
        }
    }
}
