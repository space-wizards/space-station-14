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
        var flavors = new List<FlavorPrototype>();
        if (!Resolve(uid, ref flavorProfile))
        {
            return Loc.GetString(BackupFlavorMessage);
        }

        foreach (var flavor in flavorProfile.Flavors)
        {
            if (!_prototypeManager.TryIndex<FlavorPrototype>(flavor, out var flavorProto))
            {
                continue;
            }

            flavors.Add(flavorProto);
        }

        var ev = new FlavorProfileModificationEvent(flavors);
        RaiseLocalEvent(uid, ev);
        RaiseLocalEvent(user, ev);

        flavors.AddRange(GetFlavorsFromReagents(solution, flavorProfile.IgnoreReagents));

        return FlavorsToFlavorMessage(flavors);
    }

    public string GetLocalizedFlavorsMessage(EntityUid user, Solution solution)
    {
        var flavors = GetFlavorsFromReagents(solution).ToList();
        var ev = new FlavorProfileModificationEvent(flavors);
        RaiseLocalEvent(user, ev);

        return FlavorsToFlavorMessage(flavors);
    }

    private string FlavorsToFlavorMessage(List<FlavorPrototype> flavors)
    {
        flavors.Sort((a, b) => a.FlavorType.CompareTo(b.FlavorType));

        if (flavors.Count == 1 && !string.IsNullOrEmpty(flavors[0].FlavorDescription))
        {
            return Loc.GetString("flavor-profile", ("flavor", flavors[0].FlavorDescription));
        }

        if (flavors.Count > 1)
        {
            var lastFlavor = Loc.GetString(flavors[^1].FlavorDescription);
            var allFlavors = string.Join(", ", flavors.GetRange(0, flavors.Count - 1).Select(i => Loc.GetString(i.FlavorDescription)));
            return Loc.GetString("flavor-profile", ("flavors", allFlavors), ("lastFlavor", lastFlavor));
        }

        return Loc.GetString("flavor-profile-nothing");
    }

    private IEnumerable<FlavorPrototype> GetFlavorsFromReagents(Solution solution, HashSet<string>? toIgnore = null)
    {
        foreach (var reagent in solution.Contents)
        {
            if (toIgnore != null && toIgnore.Contains(reagent.ReagentId))
            {
                continue;
            }

            var flavor = _prototypeManager.Index<ReagentPrototype>(reagent.ReagentId).Flavor;
            if (!_prototypeManager.TryIndex<FlavorPrototype>(flavor, out var flavorProto))
            {
                continue;
            }

            yield return flavorProto;
        }
    }
}

public sealed class FlavorProfileModificationEvent : EntityEventArgs
{
    public FlavorProfileModificationEvent(List<FlavorPrototype> flavors)
    {
        Flavors = flavors;
    }

    // flavorprototype has readonly semantics, so just don't store it
    public List<FlavorPrototype> Flavors { get; }
}
