using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using System.IO;
using System.Linq;
using System.Text.Json;
using Content.Server.EntityEffects.Effects;

namespace Content.Server.Corvax.GuideGenerator;
public sealed class HealthChangeReagentsJsonGenerator
{
    public static void PublishJson(StreamWriter file)
    {
        var prototype = IoCManager.Resolve<IPrototypeManager>();

        Dictionary<string, Dictionary<string, Dictionary<string, float>>> healthChangeReagents = new();

        // Сбор данных
        foreach (var reagent in prototype.EnumeratePrototypes<ReagentPrototype>())
        {
            if (reagent.Metabolisms is null) continue;

            foreach (var metabolism in reagent.Metabolisms)
            {
                foreach (HealthChange effect in metabolism.Value.Effects.Where(x => x is HealthChange))
                {
                    foreach (var damage in effect.Damage.DamageDict)
                    {
                        var damageType = damage.Key;
                        var damageChangeType = damage.Value.Float() < 0 ? "health" : "damage";

                        if (!healthChangeReagents.ContainsKey(damageType))
                        {
                            healthChangeReagents.Add(damageType, new());
                        }

                        if (!healthChangeReagents[damageType].ContainsKey(damageChangeType))
                        {
                            healthChangeReagents[damageType].Add(damageChangeType, new());
                        }

                        // Берем максимальный показатель (один реагент может наносить разный урон при разных условиях)
                        var damageChangeValueAbs = Math.Abs(damage.Value.Float() / metabolism.Value.MetabolismRate.Float()); // вычисляем показатель за 1 ед. вещества, а не 1 сек. нахождения я в организме.
                        if (healthChangeReagents[damageType][damageChangeType].TryGetValue(reagent.ID, out var previousValue))
                        {
                            healthChangeReagents[damageType][damageChangeType][reagent.ID] = Math.Max(previousValue, damageChangeValueAbs);
                        }
                        else healthChangeReagents[damageType][damageChangeType].Add(reagent.ID, damageChangeValueAbs);
                    }
                }
            }
        }

        // Сортировка
        Dictionary<string, Dictionary<string, List<string>>> healthChangeReagentsSorted = new();

        foreach (var damageType in healthChangeReagents)
        {
            foreach (var damageChangeType in damageType.Value)
            {
                foreach (var reagent in damageChangeType.Value)
                {
                    if (!healthChangeReagentsSorted.ContainsKey(damageType.Key))
                    {
                        healthChangeReagentsSorted.Add(damageType.Key, new());
                    }

                    if (!healthChangeReagentsSorted[damageType.Key].ContainsKey(damageChangeType.Key))
                    {
                        healthChangeReagentsSorted[damageType.Key].Add(damageChangeType.Key, new());
                    }

                    healthChangeReagentsSorted[damageType.Key][damageChangeType.Key].Add(reagent.Key);

                }

                healthChangeReagentsSorted[damageType.Key][damageChangeType.Key].Sort(Comparer<string>.Create((s1, s2) =>
                    -healthChangeReagents[damageType.Key][damageChangeType.Key][s1].CompareTo(healthChangeReagents[damageType.Key][damageChangeType.Key][s2])));
            }
        }



        var serializeOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
        };

        file.Write(JsonSerializer.Serialize(healthChangeReagentsSorted, serializeOptions));
    }
}

