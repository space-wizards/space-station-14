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

        flavors.Add(Loc.GetString(flavorProfile.Flavor));

        flavors.AddRange(GetFlavorsFromReagents(solution, flavorProfile.IgnoreReagents));

        var allFlavors = string.Join(", ", flavors);

        return !string.IsNullOrEmpty(allFlavors)
            ? Loc.GetString("flavor-profile", ("flavors", allFlavors))
            : Loc.GetString("flavor-profile-nothing");
    }

    public string GetLocalizedFlavorsMessage(Solution solution)
    {
        var allFlavors = string.Join(", ", GetFlavorsFromReagents(solution));
        return Loc.GetString("flavor-profile", ("flavors", allFlavors));
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
