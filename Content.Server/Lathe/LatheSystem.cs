using Content.Server.Atmos.EntitySystems;
using Content.Server.Lathe.Components;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Lathe;
using Content.Shared.Localizations;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using JetBrains.Annotations;
using Robust.Server.GameObjects;

namespace Content.Server.Lathe;

[UsedImplicitly]
public sealed class LatheSystem : SharedLatheSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly RadioSystem _radio = default!;

    /// <summary>
    /// Per-tick cache
    /// </summary>
    private readonly List<GasMixture> _environments = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LatheAnnouncingComponent, TechnologyDatabaseModifiedEvent>(OnTechnologyDatabaseModified);
        SubscribeLocalEvent<LatheHeatProducingComponent, LatheStartPrintingEvent>(OnHeatStartPrinting);
    }
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<LatheProducingComponent, LatheComponent>();
        while (query.MoveNext(out var uid, out var comp, out var lathe))
        {
            if (lathe.CurrentRecipe == null)
                continue;

            if (Timing.CurTime - comp.StartTime >= comp.ProductionLength)
                FinishProducing((uid, lathe, comp));
        }

        var heatQuery = EntityQueryEnumerator<LatheHeatProducingComponent, LatheProducingComponent, TransformComponent>();
        while (heatQuery.MoveNext(out var uid, out var heatComp, out _, out var xform))
        {
            if (Timing.CurTime < heatComp.NextSecond)
                continue;
            heatComp.NextSecond += TimeSpan.FromSeconds(1);

            var position = _transform.GetGridTilePositionOrDefault((uid, xform));
            _environments.Clear();

            if (_atmosphere.GetTileMixture(xform.GridUid, xform.MapUid, position, true) is { } tileMix)
                _environments.Add(tileMix);

            if (xform.GridUid != null)
            {
                var enumerator = _atmosphere.GetAdjacentTileMixtures(xform.GridUid.Value, position, false, true);
                while (enumerator.MoveNext(out var mix))
                {
                    _environments.Add(mix);
                }
            }

            if (_environments.Count > 0)
            {
                var heatPerTile = heatComp.EnergyPerSecond / _environments.Count;
                foreach (var env in _environments)
                {
                    _atmosphere.AddHeat(env, heatPerTile);
                }
            }
        }
    }

    private void OnHeatStartPrinting(EntityUid uid, LatheHeatProducingComponent component, LatheStartPrintingEvent args)
    {
        component.NextSecond = Timing.CurTime;
    }

    private void OnTechnologyDatabaseModified(Entity<LatheAnnouncingComponent> ent, ref TechnologyDatabaseModifiedEvent args)
    {
        if (args.NewlyUnlockedRecipes is null)
            return;

        if (!TryGetAvailableRecipes(ent.Owner, out var potentialRecipes))
            return;

        var recipeNames = new List<string>();
        foreach (var recipeId in args.NewlyUnlockedRecipes)
        {
            if (!potentialRecipes.Contains(new(recipeId)))
                continue;

            if (!Proto.TryIndex(recipeId, out LatheRecipePrototype? recipe))
                continue;

            var itemName = GetRecipeName(recipe);
            recipeNames.Add(Loc.GetString("lathe-unlock-recipe-radio-broadcast-item", ("item", itemName)));
        }

        if (recipeNames.Count == 0)
            return;

        var message =
            recipeNames.Count > ent.Comp.MaximumItems
                ? Loc.GetString(
                    "lathe-unlock-recipe-radio-broadcast-overflow",
                    ("items", ContentLocalizationManager.FormatList(recipeNames.GetRange(0, ent.Comp.MaximumItems))),
                    ("count", recipeNames.Count)
                )
                : Loc.GetString(
                    "lathe-unlock-recipe-radio-broadcast",
                    ("items", ContentLocalizationManager.FormatList(recipeNames))
                );

        foreach (var channel in ent.Comp.Channels)
        {
            _radio.SendRadioMessage(ent.Owner, message, channel, ent.Owner, escapeMarkup: false);
        }
    }
}
