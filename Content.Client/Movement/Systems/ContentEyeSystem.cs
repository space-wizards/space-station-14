using Content.Shared.Input;
using Content.Shared.Movement.Systems;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Players;

namespace Content.Client.Movement.Systems;

public sealed class ContentEyeSystem : SharedContentEyeSystem
{
    public override void Initialize()
    {
        base.Initialize();
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ScrollUp,  new ScrollInputCmdHandler(true))
            .Bind(ContentKeyFunctions.ScrollDown, new ScrollInputCmdHandler(false))
            .Register<ContentEyeSystem>();
    }

    protected override bool CanZoom(EntityUid uid, ICommonSession session)
    {
        throw new NotImplementedException();
    }

    private sealed class ScrollInputCmdHandler : InputCmdHandler
    {
        private readonly SharedMoverController _controller;
        private readonly ShuttleButtons _button;

        public ScrollInputCmdHandler()
        {
            _controller = controller;
            _button = button;
        }

        public override bool HandleCmdMessage(ICommonSession? session, InputCmdMessage message)
        {
            if (message is not FullInputCmdMessage full || session?.AttachedEntity == null) return false;

            _controller.HandleShuttleInput(session.AttachedEntity.Value, _button, full.SubTick, full.State == BoundKeyState.Down);
            return false;
        }
    }
}
