using Content.Shared.Hands.Components;
using Content.Shared.RCD.Components;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.Player;

namespace Content.Client.RCD;

public sealed class RCDPlacementSystem : EntitySystem
{
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    bool _rotationKeyDown = false;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var player = _player.LocalSession?.AttachedEntity;

        if (!TryComp<HandsComponent>(player, out var hands))
            return;

        var uid = hands.ActiveHand?.HeldEntity;

        if (uid == null)
            return;

        var hasRCD = HasComp<RCDComponent>(uid);

        if (hasRCD && _inputManager.Contexts.ActiveContext != _inputManager.Contexts.GetContext("rcd"))
            _inputManager.Contexts.SetActiveContext("rcd");

        else if (!hasRCD && _inputManager.Contexts.ActiveContext == _inputManager.Contexts.GetContext("rcd"))
            _entitySystemManager.GetEntitySystem<InputSystem>().SetEntityContextActive();
    }
}
