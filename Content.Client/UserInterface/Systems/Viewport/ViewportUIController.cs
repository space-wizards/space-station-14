using Content.Client.UserInterface.Controls;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Systems.Viewport;

public sealed class ViewportUIController : UIController
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;

    public static readonly Vector2i ViewportSize = (EyeManager.PixelsPerMeter * 21, EyeManager.PixelsPerMeter * 15);
    private MainViewport? Viewport => UIManager.ActiveScreen?.GetWidget<MainViewport>();

    public void ReloadViewport()
    {
        if (Viewport == null)
        {
            return;
        }

        Viewport.Viewport.ViewportSize = ViewportSize;
        Viewport.Viewport.HorizontalExpand = true;
        Viewport.Viewport.VerticalExpand = true;
        _eyeManager.MainViewport = Viewport.Viewport;
    }

    public override void FrameUpdate(FrameEventArgs e)
    {
        if (Viewport == null)
        {
            return;
        }

        base.FrameUpdate(e);

        Viewport.Viewport.Eye = _eyeManager.CurrentEye;

        // verify that the current eye is not "null". Fuck IEyeManager.

        var ent = _playerMan.LocalPlayer?.ControlledEntity;
        if (_eyeManager.CurrentEye.Position != default || ent == null)
            return;

        _entMan.TryGetComponent(ent, out EyeComponent? eye);

        if (eye?.Eye == _eyeManager.CurrentEye
            && _entMan.GetComponent<TransformComponent>(ent.Value).WorldPosition == default)
            return; // nothing to worry about, the player is just in null space... actually that is probably a problem?

        // Currently, this shouldn't happen. This likely happened because the main eye was set to null. When this
        // does happen it can create hard to troubleshoot bugs, so lets print some helpful warnings:
        Logger.Warning($"Main viewport's eye is in nullspace (main eye is null?). Attached entity: {_entMan.ToPrettyString(ent.Value)}. Entity has eye comp: {eye != null}");
    }
}
