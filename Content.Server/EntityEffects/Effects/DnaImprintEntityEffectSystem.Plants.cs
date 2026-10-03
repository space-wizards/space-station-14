using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;

namespace Content.Server.EntityEffects.Effects;

public sealed partial class DnaImprintEntityEffectSystem
{
    [Dependency] private PlantSystem _plants = default!;

    private void InitializePlants()
    {
        SubscribeLocalEvent<PlantComponent, EntityEffectEvent<DnaImprint>>(OnPlantImprint);
        SubscribeLocalEvent<NPCImprintedComponent, PlantProduceSpawnedEvent>(OnProduceSpawned);
    }

    private void OnPlantImprint(Entity<PlantComponent> ent, ref EntityEffectEvent<DnaImprint> args)
    {
        if (!HasComp<NPCImprintedComponent>(ent) ||
            !_plants.TryGetTray(ent.AsNullable(), out var tray) ||
            !_solutions.TryGetSolution(tray.Owner, tray.Comp.SoilSolutionName, out _, out var solution))
            return;

        Imprint(ent, solution, args.Effect);
    }

    private void OnProduceSpawned(Entity<NPCImprintedComponent> ent, ref PlantProduceSpawnedEvent args)
    {
        if (ent.Comp.Leader == null && ent.Comp.Friendly.Count == 0 && ent.Comp.Target.Count == 0 ||
            !HasComp<HTNComponent>(args.Produce) || !IsNpc(args.Produce))
            return;

        var imprint = EnsureComp<NPCImprintedComponent>(args.Produce);
        imprint.Leader = ent.Comp.Leader;
        imprint.Friendly.UnionWith(ent.Comp.Friendly);
        imprint.Target.UnionWith(ent.Comp.Target);
        UpdateBehavior(args.Produce);
    }
}
