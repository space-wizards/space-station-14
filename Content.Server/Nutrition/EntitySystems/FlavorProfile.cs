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

    public string GetLocalizedFlavorsMessage(EntityUid uid, Solution solution,
        FlavorProfileComponent? flavorProfile = null)
    {
        var flavors = new List<string>();
        if (!Resolve(uid, ref flavorProfile))
        {
            return Loc.GetString(BackupFlavorMessage);
        }

        foreach (var flavor in flavorProfile.Flavors)
        {
            flavors.Add(Loc.GetString(flavor));
        }

        flavors.AddRange(GetFlavorsFromReagents(solution, flavorProfile.IgnoreReagents));

        return FlavorsToFlavorMessage(flavors);
    }

    public string GetLocalizedFlavorsMessage(Solution solution)
    {
        return FlavorsToFlavorMessage(GetFlavorsFromReagents(solution).ToList());
    }

    private string FlavorsToFlavorMessage(List<string> flavors)
    {
        if (flavors.Count == 1 && !string.IsNullOrEmpty(flavors[0]))
        {
            return Loc.GetString("flavor-profile", ("flavor", flavors[0]));
        }

        if (flavors.Count > 1)
        {
            var lastFlavor = flavors[^1];
            var allFlavors = string.Join(", ", flavors.GetRange(0, flavors.Count - 1));
            return Loc.GetString("flavor-profile", ("flavors", allFlavors), ("lastFlavor", lastFlavor))
        }

        return Loc.GetString("flavor-profile-nothing");
    }

    private IEnumerable<string> GetFlavorsFromReagents(Solution solution, HashSet<string>? toIgnore = null)
    {
        foreach (var reagent in solution.Contents)
        {
            if (toIgnore != null && toIgnore.Contains(reagent.ReagentId))
            {
                continue;
            }

            var desc = _prototypeManager.Index<ReagentPrototype>(reagent.ReagentId).LocalizedFlavorDescription;
            yield return desc;
        }
    }
}
