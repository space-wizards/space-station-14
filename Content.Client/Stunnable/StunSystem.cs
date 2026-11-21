using System.Numerics;
using Content.Shared.CombatMode;
using Content.Shared.Interaction;
using Content.Shared.Stunnable;
using Robust.Client.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;

namespace Content.Client.Stunnable;

public sealed partial class StunSystem : SharedStunSystem
{
    [Dependency] private SharedCombatModeSystem _combat = default!;
    [Dependency] private SpriteSystem _spriteSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StunVisualsComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<StunVisualsComponent, AppearanceChangeEvent>(OnAppearanceChanged);

        CommandBinds.Builder
            .BindAfter(EngineKeyFunctions.UseSecondary, new PointerInputCmdHandler(OnUseSecondary, true, true), typeof(SharedInteractionSystem))
            .Register<StunSystem>();
    }

    private bool OnUseSecondary(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (args.Session?.AttachedEntity is not {Valid: true} uid)
            return false;

        if (args.EntityUid != uid || !HasComp<KnockedDownComponent>(uid) || !_combat.IsInCombatMode(uid))
            return false;

        RaisePredictiveEvent(new ForceStandUpEvent());
        return true;
    }

    /// <summary>
    ///     Add stun visual layers
    /// </summary>
    private void OnComponentInit(Entity<StunVisualsComponent> entity, ref ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(entity, out var sprite))
            return;

        var spriteEntity = (entity.Owner, sprite);

        _spriteSystem.LayerMapReserve(spriteEntity, StunVisualLayers.StamCrit);
        _spriteSystem.LayerSetVisible(spriteEntity, StunVisualLayers.StamCrit, false);
        _spriteSystem.LayerSetOffset(spriteEntity, StunVisualLayers.StamCrit, new Vector2(0, 0.3125f));

        _spriteSystem.LayerSetRsi(spriteEntity, StunVisualLayers.StamCrit, entity.Comp.StarsPath);

        UpdateAppearance((entity, sprite), entity.Comp.State);
    }

    private void OnAppearanceChanged(Entity<StunVisualsComponent> entity, ref AppearanceChangeEvent args)
    {
        if (args.Sprite != null)
            UpdateAppearance((entity, args.Sprite), entity.Comp.State);
    }

    private void UpdateAppearance(Entity<SpriteComponent?> entity, string state)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        if (!_spriteSystem.LayerMapTryGet((entity, entity.Comp), StunVisualLayers.StamCrit, out var index, false))
            return;

        var visible = Appearance.TryGetData<bool>(entity, StunVisuals.SeeingStars, out var stars) && stars;

        _spriteSystem.LayerSetVisible((entity, entity.Comp), index, visible);
        _spriteSystem.LayerSetRsiState((entity, entity.Comp), index, state);
    }
}

public enum StunVisualLayers : byte
{
    StamCrit,
}
