using Content.Server.NPC;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.Forensics.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind.Components;
using Content.Shared.Pointing;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects;

public sealed partial class DnaImprintEntityEffectSystem : EntityEffectSystem<HTNComponent, DnaImprint>
{
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private HTNSystem _htn = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DNALeaderComponent, AfterPointedAtEvent>(OnPointedAt);
        SubscribeLocalEvent<NPCImprintedComponent, AttackAttemptEvent>(OnAttackAttempt);
        InitializePlants();
    }

    protected override void Effect(Entity<HTNComponent> ent, ref EntityEffectEvent<DnaImprint> args)
    {
        var effect = args.Effect;
        if (!IsNpc(ent) || !(effect.Leader || effect.Friendly || effect.Target) ||
            !TryComp<BloodstreamComponent>(ent, out var bloodstream) ||
            !_solutions.TryGetSolution(ent.Owner, bloodstream.BloodSolutionName, out _, out var solution))
            return;

        Imprint(ent, solution, effect);
    }

    private void Imprint(EntityUid uid, Content.Shared.Chemistry.Components.Solution solution, DnaImprint effect)
    {
        // Only this effect's reagent supplies DNA; ordinary blood and other imprinting drugs are unrelated.
        var dna = new HashSet<string>();
        var unknownDna = Loc.GetString("forensics-dna-unknown");
        foreach (var sample in solution.Contents)
        {
            if (sample.Reagent.Prototype != effect.Reagent || sample.Reagent.Data is not { } data)
                continue;

            foreach (var entry in data)
            {
                if (entry is DnaData donorDna && !string.IsNullOrWhiteSpace(donorDna.DNA) && donorDna.DNA != unknownDna)
                    dna.Add(donorDna.DNA);
            }
        }
        if (dna.Count == 0)
            return;

        var donors = new List<EntityUid>();
        var query = EntityQueryEnumerator<DnaComponent>();
        while (query.MoveNext(out var donor, out var donorDna))
        {
            if (donor == uid || donorDna.DNA == null || !dna.Contains(donorDna.DNA) || TerminatingOrDeleted(donor))
                continue;

            donors.Add(donor);
        }
        if (donors.Count == 0)
            return;

        var imprint = EnsureComp<NPCImprintedComponent>(uid);
        var changed = false;
        if (effect.Leader)
        {
            var leader = _random.Pick(donors);
            if (imprint.Leader != leader)
            {
                EnsureComp<DNALeaderComponent>(leader);
                imprint.Leader = leader;
                changed = true;
            }
        }
        foreach (var donor in donors)
        {
            if (effect.Friendly)
                changed |= imprint.Friendly.Add(donor);
            if (effect.Target)
                changed |= imprint.Target.Add(donor);
        }

        if (changed)
            UpdateBehavior(uid);
    }

    private void UpdateBehavior(EntityUid uid)
    {
        if (!IsNpc(uid) || !TryComp<HTNComponent>(uid, out var htn))
            return;

        htn.Blackboard.Remove<EntityUid>(NPCBlackboard.CurrentOrderedTarget);
        htn.RootTask = new HTNCompoundTask { Task = "ImprintedCompound" };
        htn.ConstantlyReplan = true;
        ResetPlan(htn);
    }

    private bool IsNpc(EntityUid uid)
        => !HasComp<ActorComponent>(uid) && !(TryComp<MindContainerComponent>(uid, out var mind) && mind.HasMind);

    private void OnPointedAt(Entity<DNALeaderComponent> ent, ref AfterPointedAtEvent args)
    {
        var query = EntityQueryEnumerator<NPCImprintedComponent, HTNComponent>();
        while (query.MoveNext(out var uid, out var imprint, out var htn))
        {
            if (!IsNpc(uid) || imprint.Leader != ent.Owner || uid == args.Pointed || imprint.Friendly.Contains(args.Pointed))
                continue;
            htn.Blackboard.SetValue(NPCBlackboard.CurrentOrderedTarget, args.Pointed);
            ResetPlan(htn);
        }
    }

    private void OnAttackAttempt(Entity<NPCImprintedComponent> ent, ref AttackAttemptEvent args)
    {
        if (IsNpc(ent) && args.Target is { } target && ent.Comp.Friendly.Contains(target))
            args.Cancel();
    }

    private void ResetPlan(HTNComponent htn)
    {
        htn.PlanningToken?.Cancel();
        htn.PlanningToken = null;
        htn.PlanningJob = null;
        if (htn.Plan != null)
        {
            _htn.ShutdownTask(htn.Plan.CurrentOperator, htn.Blackboard, HTNOperatorStatus.Failed);
            _htn.ShutdownPlan(htn);
        }
        _htn.Replan(htn);
    }
}
