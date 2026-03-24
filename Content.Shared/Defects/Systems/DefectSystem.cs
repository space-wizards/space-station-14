using Content.Shared.Defects.Components;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.Defects.Systems;

/// <summary>
/// Rolls DefectComponent.Prob for each defect on entities with RandomDefectsComponent
/// at MapInit, removing defects that fail. Then updates the entity name and description
/// to reflect surviving defects (name prefix by count, description suffix listing labels).
///
/// Name prefix by defect count:
///   0 defects → "Like New"
///   1 defect  → "Used"
///   2 defects → "Worn"
///   3+ defects → "Rusty"
/// </summary>
public sealed class DefectSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomDefectsComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<RandomDefectsComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient)
            return;

        // Roll for each possible defect
        var toRemove = new List<Type>();
        foreach (var comp in AllComps(ent.Owner))
        {
            // Grab all the defects components
            if (comp is not DefectComponent defect)
                continue;

            // Data check make sure probability isnt impossible value
            if (defect.Prob >= 1.0f)
                continue;

            // Roll the probability
            if (!_random.Prob(defect.Prob))
                toRemove.Add(comp.GetType());
        }
        foreach (var type in toRemove)
            RemComp(ent.Owner, type);

        // Collect surviving defect labels for naming.
        var labels = new List<string>();
        foreach (var comp in AllComps(ent.Owner))
        {
            if (comp is DefectComponent defect && !string.IsNullOrEmpty(defect.DefectLabel))
                labels.Add(defect.DefectLabel);
        }

        var prefix = labels.Count switch
        {
            0 => "Like New",
            1 => "Used",
            2 => "Worn",
            _ => "Rusty"
        };

        var meta = MetaData(ent.Owner);
        _metaData.SetEntityName(ent.Owner, $"{prefix} {meta.EntityName}", meta);

        if (labels.Count > 0)
        {
            var defectLine = $"\n\nDefects: {string.Join(", ", labels)}.";
            _metaData.SetEntityDescription(ent.Owner, meta.EntityDescription + defectLine, meta);
        }
    }
}
