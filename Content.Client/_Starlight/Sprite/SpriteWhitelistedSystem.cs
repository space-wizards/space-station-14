using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Client._Starlight.Tag;
using Content.Client.Silicons.StationAi;
using Content.Shared._Starlight.OnCollide;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._Starlight.Sprite;
public sealed partial class SpriteWhitelistedSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpriteWhitelistedComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<InvalidateLocalEntityTagEvent>(OnInvalidate);
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnAttached);
    }

    private void OnAttached(LocalPlayerAttachedEvent ev)
        => UpdateAllWhitelistedSprites(ev.Entity);

    private void OnInvalidate(ref InvalidateLocalEntityTagEvent ev)
    {
        if (_player.LocalEntity is not { } player)
            return;

        UpdateAllWhitelistedSprites(player);
    }

    private void OnAppearanceChange(Entity<SpriteWhitelistedComponent> ent, ref AppearanceChangeEvent args)
    {
        if (_player.LocalEntity is not { } player
            || !TryComp<SpriteComponent>(ent, out var sprite))
            return;

        UpdateSpriteLayersForWhitelist((ent, ent, sprite), player);
    }

    private void UpdateAllWhitelistedSprites(EntityUid player)
    {
        var query = EntityQueryEnumerator<SpriteWhitelistedComponent, SpriteComponent>();
        while (query.MoveNext(out var ent, out var comp, out var sprite))
            UpdateSpriteLayersForWhitelist((ent, comp, sprite), player);
    }

    private void UpdateSpriteLayersForWhitelist(Entity<SpriteWhitelistedComponent, SpriteComponent> ent, EntityUid player)
    {
        var layers = _whitelistSystem.IsWhitelistPassOrNull(ent.Comp1.LocalEntityWhitelist, player)
            ? ent.Comp1.PassedLayers
            : ent.Comp1.FailedLayers;

        foreach (var (layer, state) in layers)
            _sprite.LayerSetData((ent.Owner, ent.Comp2), layer, state);
    }
}
