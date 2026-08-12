using Content.Client.Examine;
using Content.Shared.Input;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;

namespace Content.Client.NamePeek;

public sealed partial class NamePeekSystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private ExamineSystem _examine = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;

    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery = default!;
    [Dependency] private EntityQuery<TransformComponent> _transformQuery = default!;
    [Dependency] private EntityQuery<MobStateComponent> _mobstateQuery = default!;

    public bool Held;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        UpdatesOutsidePrediction = true;

        _overlay.AddOverlay(new NamePeekOverlay(
            _lookup,
            _sprite,
            _transform,
            this,
            _examine,
            _spriteQuery,
            _transformQuery,
            _mobstateQuery));

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ExamineNames, new PointerInputCmdHandler(OnExamineNames, ignoreUp: false, outsidePrediction: true))
            .Register<NamePeekSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<NamePeekOverlay>();
    }

    private bool OnExamineNames(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (_player.LocalEntity == null)
            return false;

        switch (args.State)
        {
            case BoundKeyState.Down:
                Held = true;
                break;
            case BoundKeyState.Up:
                Held = false;
                break;
        }

        return true;
    }
}
