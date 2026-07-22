// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Blink;
using Content.Shared.Interaction;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;

namespace Content.Client.DeadSpace.Blink;

public sealed class BlinkSystem : SharedBlinkSystem
{
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IOverlayManager _overlays = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private BlinkRangeOverlay _overlay = default!;
    private BlinkBloodTrailOverlay _bloodTrail = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay = new BlinkRangeOverlay(EntityManager, _player, TryGetViewingItem);
        _bloodTrail = new BlinkBloodTrailOverlay();
        _overlays.AddOverlay(_overlay);
        _overlays.AddOverlay(_bloodTrail);
        SubscribeNetworkEvent<BlinkDashVisualEvent>(OnDashVisual);

        CommandBinds.Builder
            .BindBefore(EngineKeyFunctions.UseSecondary,
                new PointerInputCmdHandler(OnSecondary, false, true),
                typeof(SharedInteractionSystem))
            .Register<BlinkSystem>();
    }

    public override void Shutdown()
    {
        CommandBinds.Unregister<BlinkSystem>();
        _overlays.RemoveOverlay(_overlay);
        _overlays.RemoveOverlay(_bloodTrail);
        base.Shutdown();
    }

    private void OnDashVisual(BlinkDashVisualEvent args)
    {
        var user = GetEntity(args.User);
        if (!Exists(user))
            return;

        _bloodTrail.Start(user, args.Duration);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        _bloodTrail.Update(EntityManager, frameTime);
    }

    private bool OnSecondary(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (args.State != BoundKeyState.Down ||
            !_input.IsKeyDown(Keyboard.Key.Alt) ||
            args.Session?.AttachedEntity is not { } user ||
            !TryFindItem(user, out var item, out _))
            return false;

        RaiseNetworkEvent(new BlinkRequestEvent(GetNetEntity(item), GetNetCoordinates(args.Coordinates)));
        return true;
    }

    private bool TryGetViewingItem(EntityUid user, out EntityUid item, out BlinkItemComponent component)
    {
        if (TryFindItem(user, out item, out component) && component.Targeting)
            return true;

        item = default;
        component = default!;
        return false;
    }
}
