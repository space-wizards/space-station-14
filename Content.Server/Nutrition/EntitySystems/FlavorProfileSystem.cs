using System.Linq;
using System.Text;
using Content.Server.Nutrition.Components;
using Content.Shared.CCVar;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Microsoft.VisualBasic;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server.Nutrition.EntitySystems;

/// <summary>
///     Deals with flavor profiles when you eat something.
/// </summary>
public sealed class FlavorProfileSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    // [Dependency] private readonly IConfigurationManager _configManager = default!; TODO: Flavor limit.

    private const string BackupFlavorMessage = "flavor-profile-nothing";

    public string GetLocalizedFlavorsMessage(EntityUid uid, EntityUid user, Solution solution,
        FlavorProfileComponent? flavorProfile = null)
    {
        var flavors = new HashSet<string>();
        if (!Resolve(uid, ref flavorProfile))
        {
            return Loc.GetString(BackupFlavorMessage);
        }

        flavors.UnionWith(flavorProfile.Flavors);
        flavors.UnionWith(GetFlavorsFromReagents(solution, flavorProfile.IgnoreReagents));

        var ev = new FlavorProfileModificationEvent(user, flavors);
        RaiseLocalEvent(ev);
        RaiseLocalEvent(uid, ev);
        RaiseLocalEvent(user, ev);

        return FlavorsToFlavorMessage(flavors);
    }

    public string GetLocalizedFlavorsMessage(EntityUid user, Solution solution)
    {
        var flavors = GetFlavorsFromReagents(solution);
        var ev = new FlavorProfileModificationEvent(user, flavors);
        RaiseLocalEvent(user, ev, true);

        return FlavorsToFlavorMessage(flavors);
    }

    private string FlavorsToFlavorMessage(HashSet<string> flavorSet)
    {
        var flavors = new List<FlavorPrototype>();
        foreach (var flavor in flavorSet)
        {
            if (!_prototypeManager.TryIndex<FlavorPrototype>(flavor, out var flavorPrototype))
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
            return Loc.GetString("flavor-profile", ("flavors", allFlavors), ("lastFlavor", lastFlavor));
        }

        return Loc.GetString("flavor-profile-nothing");
    }

    private HashSet<string> GetFlavorsFromReagents(Solution solution, HashSet<string>? toIgnore = null)
    {
        var flavors = new HashSet<string>();
        foreach (var reagent in solution.Contents)
        {
            if (toIgnore != null && toIgnore.Contains(reagent.ReagentId))
            {
                continue;
            }

            var flavor = _prototypeManager.Index<ReagentPrototype>(reagent.ReagentId).Flavor;

            flavors.Add(flavor);
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
