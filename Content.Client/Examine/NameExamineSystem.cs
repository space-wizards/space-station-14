using Content.Shared.IdentityManagement;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;

namespace Content.Client.Examine;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class NameExamineSystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IdentitySystem _identity = default!;

    public bool Held;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        UpdatesOutsidePrediction = true;

        _overlay.AddOverlay(new NameExamineOverlay(
            _sprite,
            _transform,
            this));

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ExamineNames, new PointerInputCmdHandler(OnExamineNames, ignoreUp: false, outsidePrediction: true))
            .Register<NameExamineSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<NameExamineOverlay>();
    }

    private bool OnExamineNames(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (_player.LocalEntity is not { } player)
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
