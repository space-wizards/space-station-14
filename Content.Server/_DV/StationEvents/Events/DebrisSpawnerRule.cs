/*
* Delta-V - This file is licensed under AGPLv3
* Copyright (c) 2024 Delta-V Contributors
* See AGPLv3.txt for details.
*/

using Content.Server.GameTicking.Rules;
using Content.Server.Station.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.CCVar;
using Content.Shared.Salvage;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.StationEvents.Events;

public sealed class DebrisSpawnerRule : StationEventSystem<DebrisSpawnerRuleComponent>
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DebrisSpawnerRuleComponent, RuleLoadedGridsEvent>(OnLoadedGrids);
    }

    private void OnLoadedGrids(Entity<DebrisSpawnerRuleComponent> ent, ref RuleLoadedGridsEvent args)
    {
        if (_config.GetCVar<bool>(CCVars.WorldgenEnabled))
            return;

        // get world AABBs of every grid that was loaded, probably just 1 anyway
        var boxes = new List<Box2>(args.Grids.Count);
        foreach (var gridId in args.Grids)
        {
            var grid = Comp<MapGridComponent>(gridId);
            var aabb = Transform(gridId).WorldMatrix.TransformBox(grid.LocalAABB);
            boxes.Add(aabb);
        }

        // fetch all the salvage maps that can be picked
        var salvageMaps = _proto.EnumeratePrototypes<SalvageMapPrototype>().ToList();

        // spawn them!
        for (var i = 0; i < ent.Comp.Count; i++)
        {
            var aabb = RobustRandom.Pick(boxes);
            var dist = MathF.Max(aabb.Height / 2f, aabb.Width / 2f) * ent.Comp.DistanceModifier;

            var offset = RobustRandom.NextVector2(dist, dist * 2.5f);
            var randomer = RobustRandom.NextVector2(dist, dist * 5f); //Second random vector to ensure the outpost isn't perfectly centered in the debris field
            var options = new MapLoadOptions
            {
                Offset = aabb.Center + offset + randomer,
                LoadMap = false,
            };

            var salvage = RobustRandom.PickAndTake(salvageMaps);
            _mapLoader.Load(args.Map, salvage.MapPath.ToString(), options);
        }
    }
}
