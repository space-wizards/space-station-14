using System.Numerics;
using Content.Shared.CCVar;
using Content.Shared.NewPlayer;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Client.NewPlayer;

public sealed partial class NewPlayerVisualizerSystem : VisualizerSystem<NewPlayerLabelComponent>
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IConfigurationManager _configManager = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private EntityQuery<SeeNewPlayersComponent> _seeNewPlayersQuery;
    private bool _showPlayerIcons;

    public override void Initialize()
    {
        base.Initialize();

        _seeNewPlayersQuery = GetEntityQuery<SeeNewPlayersComponent>();

        Subs.CVar(_configManager, CCVars.ShowNewPlayerIcons, NewPlayerIconsOptionChanged, true);
    }

    [SubscribeLocalEvent]
    private void OnComponentInit(Entity<SeeNewPlayersComponent> ent, ref ComponentInit args)
    {
        UpdateAllAppearance();
    }

    [SubscribeLocalEvent]
    private void OnComponentShutdown(Entity<SeeNewPlayersComponent> ent, ref ComponentShutdown args)
    {
        UpdateAllAppearance();
    }

    [SubscribeLocalEvent]
    private void OnComponentInit(Entity<NewPlayerLabelComponent> ent, ref ComponentInit args)
    {
        UpdateAppearance(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnComponentShutdown(Entity<NewPlayerLabelComponent> ent, ref ComponentShutdown args)
    {
        if (!_sprite.LayerMapTryGet(ent.Owner, NewPlayerLayers.Layer, out var layer, false))
            return;

        _sprite.LayerSetVisible(ent.Owner, layer, false);
    }

    [SubscribeLocalEvent]
    private void OnSeeNewPlayersLocalAttached(Entity<SeeNewPlayersComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        UpdateAllAppearance();
    }

    [SubscribeLocalEvent]
    private void OnSeeNewPlayersLocalDetached(Entity<SeeNewPlayersComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        UpdateAllAppearance();
    }

    protected override void OnAppearanceChange(EntityUid uid, NewPlayerLabelComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        UpdateAppearance((uid, args.Component, args.Sprite));
    }

    private void UpdateAllAppearance()
    {
        var query = AllEntityQuery<NewPlayerLabelComponent, AppearanceComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out _, out var appearance, out var sprite))
        {
            UpdateAppearance((uid, appearance, sprite));
        }
    }

    private void UpdateAppearance(Entity<AppearanceComponent?, SpriteComponent?> ent)
    {
        var spriteEntity = (ent.Owner, ent.Comp2);

        if (!_sprite.LayerMapTryGet(spriteEntity, NewPlayerLayers.Layer, out var layer, false))
            return;

        if (!_showPlayerIcons ||
            !_seeNewPlayersQuery.TryComp(_player.LocalEntity, out var see) ||
            see.LifeStage >= ComponentLifeStage.Stopping ||
            !AppearanceSystem.TryGetData(ent, NewPlayerLayers.Layer, out NewPlayerVisuals visual, ent) ||
            !see.LabelSprites.TryGetValue(visual, out var state))
        {
            _sprite.LayerSetVisible(spriteEntity, layer, false);
            return;
        }

        _sprite.LayerSetSprite(spriteEntity, layer, state);
        _sprite.LayerSetVisible(spriteEntity, layer, true);
        _sprite.LayerSetOffset(spriteEntity, layer, new Vector2(0, 0.21f));
    }

    private void NewPlayerIconsOptionChanged(bool enabled)
    {
        _showPlayerIcons = enabled;
        UpdateAllAppearance();
    }
}
