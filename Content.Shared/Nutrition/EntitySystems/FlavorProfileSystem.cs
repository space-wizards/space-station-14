using System.Linq;
using Content.Shared.CCVar;
using Content.Shared.Chemistry.Components;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Shared.Nutrition.EntitySystems;

/// <summary>
///     Deals with flavor profiles when you eat something.
/// </summary>
public sealed class FlavorProfileSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;

    private const string BackupFlavorMessage = "flavor-profile-unknown";

    private int FlavorLimit => _configManager.GetCVar(CCVars.FlavorLimit);

    public string GetLocalizedFlavorsMessage(EntityUid uid, EntityUid user, Solution solution,
        FlavorProfileComponent? flavorProfile = null)
    {
        if (!Resolve(uid, ref flavorProfile, false))
        {
            return Loc.GetString(BackupFlavorMessage);
        }

        var flavors = new HashSet<string>(flavorProfile.Flavors);
        flavors.UnionWith(GetFlavorsFromReagents(solution, FlavorLimit - flavors.Count, flavorProfile.IgnoreReagents));

        var ev = new FlavorProfileModificationEvent(user, flavors);
        RaiseLocalEvent(ev);
        RaiseLocalEvent(uid, ev);
        RaiseLocalEvent(user, ev);

        return FlavorsToFlavorMessage(flavors);
    }

    public string GetLocalizedFlavorsMessage(EntityUid user, Solution solution)
    {
        var flavors = GetFlavorsFromReagents(solution, FlavorLimit);
        var ev = new FlavorProfileModificationEvent(user, flavors);
        RaiseLocalEvent(user, ev, true);

        return FlavorsToFlavorMessage(flavors);
    }

    private string FlavorsToFlavorMessage(HashSet<string> flavorSet)
    {
        var flavors = new List<FlavorPrototype>();
        foreach (var flavor in flavorSet)
        {
            if (string.IsNullOrEmpty(flavor) || !_prototypeManager.TryIndex<FlavorPrototype>(flavor, out var flavorPrototype))
            {
                continue;
            }

            flavors.Add(flavorPrototype);
        }

        flavors.Sort((a, b) => a.FlavorType.CompareTo(b.FlavorType));

        if (flavors.Count == 1 && !string.IsNullOrEmpty(flavors[0].FlavorDescription))
        {
            return Loc.GetString("flavor-profile", ("flavor", Loc.GetString(flavors[0].FlavorDescription)));
        }

        if (flavors.Count > 1)
        {
            var lastFlavor = Loc.GetString(flavors[^1].FlavorDescription);
            var allFlavors = string.Join(", ", flavors.GetRange(0, flavors.Count - 1).Select(i => Loc.GetString(i.FlavorDescription)));
            return Loc.GetString("flavor-profile-multiple", ("flavors", allFlavors), ("lastFlavor", lastFlavor));
        }

        return Loc.GetString(BackupFlavorMessage);
    }

    private HashSet<string> GetFlavorsFromReagents(Solution solution, int desiredAmount, HashSet<string>? toIgnore = null)
    {
        var flavors = new HashSet<string>();
        foreach (var (reagent, quantity) in solution.GetReagentPrototypes(_prototypeManager))
        {
            if (toIgnore != null && toIgnore.Contains(reagent.ID))
            {
                continue;
            }

            if (flavors.Count == desiredAmount)
            {
                break;
            }

            // don't care if the quantity is negligible
            if (quantity < reagent.FlavorMinimum)
            {
                continue;
            }

            if (reagent.Flavor != null)
                flavors.Add(reagent.Flavor);
        }

        return flavors;
    }
}

public sealed class FlavorProfileModificationEvent : EntityEventArgs
{
    public FlavorProfileModificationEvent(EntityUid user, HashSet<string> flavors)
    {
        User = user;
        Flavors = flavors;
    }

    public EntityUid User { get; }
    public HashSet<string> Flavors { get; }
}
