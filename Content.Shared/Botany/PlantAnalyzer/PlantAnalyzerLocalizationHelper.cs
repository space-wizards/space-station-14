using Content.Shared.Atmos;
using Content.Shared.Atmos.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Localizations;
using Robust.Shared.Prototypes;

namespace Content.Shared.Botany.PlantAnalyzer;

public sealed class PlantAnalyzerLocalizationHelper
{
    public static string GasesToLocalizedStrings(List<Gas> gases, IPrototypeManager protMan)
    {
        if (gases.Count == 0)
            return "";

        List<int> gasIds = [];
        foreach (var gas in gases)
            gasIds.Add((int)gas);

        List<string> gasesLoc = [];
        foreach (var gas in protMan.EnumeratePrototypes<GasPrototype>())
            if (gasIds.Contains(int.Parse(gas.ID)))
                gasesLoc.Add(Loc.GetString(gas.Name));

        return ContentLocalizationManager.FormatList(gasesLoc);
    }

    public static string ChemicalsToLocalizedStrings(List<string> ids, IPrototypeManager protMan)
    {
        if (ids.Count == 0)
            return "";

        List<string> locStrings = [];
        foreach (var id in ids)
            locStrings.Add(protMan.TryIndex<ReagentPrototype>(id, out var prototype) ? prototype.LocalizedName : id);

        return ContentLocalizationManager.FormatList(locStrings);
    }

    public static string ProduceToLocalizedStrings(List<string> ids, IPrototypeManager protMan)
    {
        if (ids.Count == 0)
            return "";

        List<string> locStrings = [];
        foreach (var id in ids)
            locStrings.Add(protMan.TryIndex<EntityPrototype>(id, out var prototype) ? prototype.Name : id);

        return ContentLocalizationManager.FormatListToOr(locStrings);
    }
}
