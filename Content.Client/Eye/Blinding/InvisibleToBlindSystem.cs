using Content.Client.Chemistry.Visualizers;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Robust.Client.Player;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client.Eye.Blinding;

/// <summary>
/// System responsible for hiding entities that should not obstruct players field of vision/sesne
/// when their eyes are closed
/// </summary>
public sealed class InvisibleToBlindSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InvisibleToBlindComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<InvisibleToBlindComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<InvisibleToBlindComponent, AppearanceChangeEvent>(OnAppearanceChange, after: [typeof(SmokeVisualizerSystem)]);

        SubscribeLocalEvent<EyeClosingComponent, EyelidsChangeStateEvent>(OnEyelidsChangeState);
    }

    private void OnComponentStartup(Entity<InvisibleToBlindComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        ent.Comp.OldVisible = sprite.Visible;
        ent.Comp.OldAlpha = sprite.Color.A;

        if (!TryComp<EyeClosingComponent>(_playerManager.LocalEntity, out var eyeClosing))
            return;

        SetVisibility(ent, !eyeClosing.EyesClosed);
    }

    private void OnComponentShutdown(Entity<InvisibleToBlindComponent> ent, ref ComponentShutdown args)
    {
        SetVisibility(ent, true);
    }

    private void OnAppearanceChange(Entity<InvisibleToBlindComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        if (args.Sprite == null)
            return;

        if (!TryComp<EyeClosingComponent>(_playerManager.LocalEntity, out var eyeClosing))
            return;

        SetVisibility(ent, !eyeClosing.EyesClosed);
    }

    private void OnEyelidsChangeState(Entity<EyeClosingComponent> ent, ref EyelidsChangeStateEvent args)
    {
        var query = EntityQueryEnumerator<InvisibleToBlindComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            SetVisibility((uid, comp), !args.EyelidsClosed);
        }
    }

    private void SetVisibility(Entity<InvisibleToBlindComponent, SpriteComponent?> ent, bool visible)
    {
        if (!Resolve(ent.Owner, ref ent.Comp2))
            return;

        var (alpha, visibility) = visible switch
        {
            true => (ent.Comp2.Color.WithAlpha(ent.Comp1.OldAlpha), ent.Comp1.OldVisible),
            false => (ent.Comp2.Color.WithAlpha(ent.Comp1.Alpha), false),
        };

        _spriteSystem.SetColor((ent.Owner, ent.Comp2), alpha);
        if (!ent.Comp1.Visible)
            _spriteSystem.SetVisible(ent.Owner, visibility);
    }
}
