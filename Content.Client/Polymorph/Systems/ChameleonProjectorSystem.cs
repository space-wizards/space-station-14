using Content.Client.Smoking;
using Content.Shared.Effects;
using Content.Shared.Chemistry.Components;
using Content.Shared.Polymorph.Components;
using Content.Shared.Polymorph.Systems;
using Robust.Client.GameObjects;

namespace Content.Client.Polymorph.Systems;

public sealed class ChameleonProjectorSystem : SharedChameleonProjectorSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private EntityQuery<AppearanceComponent> _appearanceQuery;
    private EntityQuery<ChameleonDisguisedComponent> _disguisedQuery;
    private EntityQuery<SpriteComponent> _spriteQuery;

    private List<NetEntity> _toFlash = new();

    public override void Initialize()
    {
        base.Initialize();

        _appearanceQuery = GetEntityQuery<AppearanceComponent>();
        _disguisedQuery = GetEntityQuery<ChameleonDisguisedComponent>();
        _spriteQuery = GetEntityQuery<SpriteComponent>();

        SubscribeLocalEvent<ChameleonDisguiseComponent, AfterAutoHandleStateEvent>(OnHandleState);

        SubscribeLocalEvent<ChameleonDisguisedComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ChameleonDisguisedComponent, ComponentShutdown>(OnShutdown);

        SubscribeAllEvent<ColorFlashEffectEvent>(OnColorFlashEffect);
    }

    private void OnHandleState(Entity<ChameleonDisguiseComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        CopyComp<SpriteComponent>(ent);
        CopyComp<GenericVisualizerComponent>(ent);
        CopyComp<SolutionContainerVisualsComponent>(ent);
        CopyComp<BurnStateVisualsComponent>(ent);

        // reload appearance to hopefully prevent any invisible layers
        if (_appearanceQuery.TryComp(ent, out var appearance))
            _appearance.QueueUpdate(ent, appearance);
    }

    private void OnStartup(Entity<ChameleonDisguisedComponent> ent, ref ComponentStartup args)
    {
        if (!_spriteQuery.TryComp(ent, out var sprite))
            return;

        ent.Comp.WasVisible = sprite.Visible;
        sprite.Visible = false;
    }

    private void OnShutdown(Entity<ChameleonDisguisedComponent> ent, ref ComponentShutdown args)
    {
        if (_spriteQuery.TryComp(ent, out var sprite))
            sprite.Visible = ent.Comp.WasVisible;
    }

    private void OnColorFlashEffect(ColorFlashEffectEvent args)
    {
        _toFlash.Clear();
        foreach (var nent in args.Entities)
        {
            var ent = GetEntity(nent);
            if (!_disguisedQuery.TryComp(ent, out var disguised))
                continue;

            // prevent recursion
            var disguise = disguised.Disguise;
            if (_disguisedQuery.HasComp(disguise))
                continue;

            _toFlash.Add(GetNetEntity(disguise));
        }

        if (_toFlash.Count == 0)
            return;

        // relay the flash effect to the disguise when you get hit
        args.Entities = _toFlash;
        RaiseLocalEvent(args);
    }
}
