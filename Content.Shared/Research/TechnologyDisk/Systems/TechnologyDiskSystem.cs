using System.Linq;
using Content.Shared.Cargo;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Lathe;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using Content.Shared.Research.Systems;
using Content.Shared.Research.TechnologyDisk.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.Research.TechnologyDisk.Systems;

public sealed class TechnologyDiskSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedResearchSystem _research = default!;
    [Dependency] private readonly SharedLatheSystem _lathe = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    /// <summary>
    /// Mapping of disk tiers to disk prices.
    /// </summary>
    private readonly Dictionary<int, int> _diskPricePerTier = new()
    {
        [1] = 100,
        [2] = 500,
        [3] = 1500
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TechnologyDiskComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TechnologyDiskComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<TechnologyDiskComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<TechnologyDiskComponent, PriceCalculationEvent>(OnPriceCalculation);
    }

    private void OnMapInit(Entity<TechnologyDiskComponent> ent, ref MapInitEvent args)
    {
        var uid = (EntityUid)ent;

        TryPickAndSetRecipe(ent);
        TrySetVisuals(ent, uid);
    }

    /// <summary>
    /// Attempts to pick and set a random recipe as the chosen one.
    /// If the disk already has recipes, does nothing.
    /// </summary>
    private void TryPickAndSetRecipe(Entity<TechnologyDiskComponent> ent)
    {
        if (ent.Comp.Recipes != null)
            return;

        var tier = ent.Comp.Tier ?? TryPickAndSetTier(ent);
        if (tier == null)
            return;

        var discipline = ent.Comp.Discipline ?? TryPickAndSetDiscipline(ent);
        if (discipline == null)
            return;

        //get a list of every distinct recipe in all the technologies.
        var recipes = new HashSet<ProtoId<LatheRecipePrototype>>();
        foreach (var tech in _protoMan.EnumeratePrototypes<TechnologyPrototype>())
        {
            if (tech.Tier != tier || tech.Discipline != discipline)
                continue;

            recipes.UnionWith(tech.RecipeUnlocks);
        }

        if (recipes.Count == 0)
        {
            Log.Error($"Failed to pick recipe for a tech disk: no suitable recipes were found");
            return;
        }

        ent.Comp.Recipes = [];
        ent.Comp.Recipes.Add(_random.Pick(recipes));
    }

    /// <summary>
    /// Attempts to pick and set a random tier as the chosen one.
    /// </summary>
    private int? TryPickAndSetTier(Entity<TechnologyDiskComponent> ent)
    {
        if (!_protoMan.TryIndex(ent.Comp.TierWeightPrototype, out var tierWeights))
        {
            Log.Error($"Failed to pick tier for a tech disk: disk tier weights prototype '{ent.Comp.TierWeightPrototype}' not found");
            return null;
        }

        var tier = int.Parse(tierWeights.Pick());
        ent.Comp.Tier = tier;
        return tier;
    }

    /// <summary>
    /// Attempts to pick and set a random discipline as the chosen one.
    /// </summary>
    private ProtoId<TechDisciplinePrototype>? TryPickAndSetDiscipline(Entity<TechnologyDiskComponent> ent)
    {
        var disciplinePool = _protoMan.EnumeratePrototypes<TechDisciplinePrototype>().ToArray();
        if (disciplinePool.Length == 0)
        {
            Log.Error("Failed to pick discipline for a tech disk: no discipline prototypes were found");
            return null;
        }

        var discipline = _random.Pick(disciplinePool);
        ent.Comp.Discipline = discipline;
        return discipline;
    }

    /// <summary>
    /// Attempts to set tier and discipline visuals based on chosen tier and discipline.
    /// </summary>
    private void TrySetVisuals(Entity<TechnologyDiskComponent> ent, EntityUid uid)
    {
        TrySetTierVisuals(ent, uid);
        TrySetDisciplineVisuals(ent, uid);
    }

    /// <summary>
    /// Attempts to set tier visuals based on chosen tier.
    /// </summary>
    private void TrySetTierVisuals(Entity<TechnologyDiskComponent> ent, EntityUid uid)
    {
        var tier = ent.Comp.Tier;
        if (!tier.HasValue)
            return;

        _appearance.SetData(uid, TechDiskVisuals.Tier, tier.Value);
    }

    /// <summary>
    /// Attempts to set discipline visuals based on chosen discipline.
    /// </summary>
    private void TrySetDisciplineVisuals(Entity<TechnologyDiskComponent> ent, EntityUid uid)
    {
        if (!_protoMan.TryIndex(ent.Comp.Discipline, out var discipline))
            return;

        _appearance.SetData(uid, TechDiskVisuals.Discipline, discipline.ID);
    }

    private void OnAfterInteract(Entity<TechnologyDiskComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!HasComp<ResearchServerComponent>(target) || !TryComp<TechnologyDatabaseComponent>(target, out var database))
            return;

        if (ent.Comp.Recipes != null)
        {
            foreach (var recipe in ent.Comp.Recipes)
            {
                _research.AddLatheRecipe(target, recipe, database);
            }
        }
        _popup.PopupClient(Loc.GetString("tech-disk-inserted"), target, args.User);
        PredictedQueueDel(ent.Owner);
        args.Handled = true;
    }

    private void OnExamine(Entity<TechnologyDiskComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.Tier != null
              && ent.Comp.Discipline != null
              && _protoMan.TryIndex(ent.Comp.Discipline, out var disciplineProto))
        {
            var desc = Loc.GetString("tech-disk-examine-desc",
                ("tier", ent.Comp.Tier),
                ("branch", Loc.GetString(disciplineProto.Name))
            );

            args.PushMarkup(desc);
        }
        else
        {
            args.PushMarkup(Loc.GetString("tech-disk-examine-desc-unknown"));
        }

        var message = Loc.GetString("tech-disk-examine-none");
        if (ent.Comp.Recipes != null && ent.Comp.Recipes.Count > 0)
        {
            var prototype = _protoMan.Index(ent.Comp.Recipes[0]);
            message = Loc.GetString("tech-disk-examine", ("result", _lathe.GetRecipeName(prototype)));

            if (ent.Comp.Recipes.Count > 1) //idk how to do this well. sue me.
                message += " " + Loc.GetString("tech-disk-examine-more");
        }
        args.PushMarkup(message);
    }

    private void OnPriceCalculation(Entity<TechnologyDiskComponent> ent, ref PriceCalculationEvent args)
    {
        if(!ent.Comp.Tier.HasValue)
            return;

        var tier = ent.Comp.Tier.Value;

        if (!_diskPricePerTier.TryGetValue(tier, out var price))
            return;

        args.Price = price;
        args.Handled = true;
    }
}

[Serializable, NetSerializable]
public enum TechDiskVisuals : byte
{
    Tier,
    Discipline
}
