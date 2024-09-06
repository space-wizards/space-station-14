using Content.Server.Atmos.EntitySystems;
using Content.Server.Heretic.Components;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.Heretic;
using Content.Shared.Maps;
using Content.Shared.Speech.Muting;
using Content.Shared.StatusEffect;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;
using System.Numerics;

namespace Content.Server.Heretic.EntitySystems;

// void path heretic exclusive
public sealed partial class AristocratSystem : EntitySystem
{
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly IRobustRandom _rand = default!;
    [Dependency] private readonly IPrototypeManager _prot = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TemperatureSystem _temp = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;

    private string _snowWallPrototype = "WallSnowCobblebrick";

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var aristocrat in EntityQuery<AristocratComponent>())
        {
            aristocrat.UpdateTimer += frameTime;

            if (aristocrat.UpdateTimer >= aristocrat.UpdateDelay)
            {
                Cycle((aristocrat.Owner, aristocrat));
                aristocrat.UpdateTimer = 0;
            }
        }
    }

    private void Cycle(Entity<AristocratComponent> ent)
    {
        SpawnTiles(ent);

        var mix = _atmos.GetTileMixture((ent, Transform(ent)));
        if (mix != null)
            mix.Temperature -= 50f;

        // replace certain things with their winter analogue
        var lookup = _lookup.GetEntitiesInRange(Transform(ent).Coordinates, ent.Comp.Range);
        foreach (var look in lookup)
        {
            if (HasComp<HereticComponent>(look) || HasComp<GhoulComponent>(look))
                continue;

            if (TryComp<TemperatureComponent>(look, out var temp))
                _temp.ChangeHeat(look, -100f, true, temp);

            _statusEffect.TryAddStatusEffect<MutedComponent>(look, "Muted", TimeSpan.FromSeconds(5), true);

            if (TryComp<TagComponent>(look, out var tag))
            {
                var tags = tag.Tags;

                // replace walls with snow ones
                if (_rand.Prob(.45f) && tags.Contains("Wall")
                && Prototype(look) != null && Prototype(look)!.ID != _snowWallPrototype)
                {
                    Spawn(_snowWallPrototype, Transform(look).Coordinates);
                    QueueDel(look);
                }
            }
        }
    }

    private void SpawnTiles(Entity<AristocratComponent> ent)
    {
        var xform = Transform(ent);

        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        var pos = xform.Coordinates.Position;
        var box = new Box2(pos + new Vector2(-ent.Comp.Range, -ent.Comp.Range), pos + new Vector2(ent.Comp.Range, ent.Comp.Range));
        var tilerefs = grid.GetLocalTilesIntersecting(box).ToList();

        if (tilerefs.Count == 0)
            return;

        var tiles = new List<TileRef>();
        var tiles2 = new List<TileRef>();
        foreach (var tile in tilerefs)
        {
            if (_rand.Prob(.45f))
                tiles.Add(tile);

            if (_rand.Prob(.05f))
                tiles2.Add(tile);
        }

        foreach (var tileref in tiles)
        {
            var tile = _prot.Index<ContentTileDefinition>("FloorAstroSnow");
            _tile.ReplaceTile(tileref, tile);
        }

        foreach (var tileref in tiles2)
        {
            // todo add more tile variety
        }
    }
}
