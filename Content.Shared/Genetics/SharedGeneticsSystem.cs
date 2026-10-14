using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Movement.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Genetics;

public sealed class SharedGeneticsSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private IRobustRandom _robustRandom = default!;

    public const int MaxPotentialMutations = 7;
    private const string Nucleotides = "ATGC";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GeneticMutationComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
    }

    public bool TryApplyMutation(EntityUid target, string mutationId)
    {
        return TryApplyMutation(target, mutationId, true);
    }

    public bool TryApplyMutationFromInjector(EntityUid target, string mutationId)
    {
        return TryApplyMutation(target, mutationId, false);
    }

    private bool TryApplyMutation(EntityUid target, string mutationId, bool requireDiscovery)
    {
        if (!_prototypeManager.TryIndex<GeneticMutationPrototype>(mutationId, out _))
            return false;

        var component = EnsureGenome(target);
        if (requireDiscovery && !component.PotentialMutations.Contains(mutationId))
            return false;

        if (!requireDiscovery && !component.PotentialMutations.Contains(mutationId))
        {
            component.PotentialMutations.Add(mutationId);
            component.MutationSequences[mutationId] = GenerateMutationSequence(
                _prototypeManager.Index<GeneticMutationPrototype>(mutationId).Sequence.Length,
                component.MutationSequences.Values);
            SeedKnownSequencePositions(
                component,
                mutationId,
                _prototypeManager.Index<GeneticMutationPrototype>(mutationId).Sequence.Length);
        }

        if (requireDiscovery && !component.DiscoveredMutations.Contains(mutationId))
            return false;
        if (component.ActiveMutations.Contains(mutationId))
            return false;

        component.ActiveMutations.Add(mutationId);
        Dirty(target, component);
        _movementSpeed.RefreshMovementSpeedModifiers(target);
        
        var ev = new GeneticMutationAppliedEvent(target, mutationId);
        RaiseLocalEvent(target, ref ev);
        
        return true;
    }

    public bool TryRemoveMutation(EntityUid target, string mutationId)
    {
        if (!TryComp<GeneticMutationComponent>(target, out var component))
            return false;

        if (!component.ActiveMutations.Remove(mutationId))
            return false;

        Dirty(target, component);
        _movementSpeed.RefreshMovementSpeedModifiers(target);
        
        var ev = new GeneticMutationRemovedEvent(target, mutationId);
        RaiseLocalEvent(target, ref ev);
        
        return true;
    }

    public bool TryGuessMutation(EntityUid target, string mutationId, string sequence)
    {
        if (!TryGetMutationSequence(target, mutationId, out var actualSequence))
            return false;

        var complement = new string(actualSequence.Select(GeneticMutationPrototype.GetComplement).ToArray());
        return TryGuessMutation(target, mutationId, sequence, complement);
    }

    public bool TryGuessMutation(EntityUid target, string mutationId, string sequence, string complementSequence)
    {
        if (!_prototypeManager.TryIndex<GeneticMutationPrototype>(mutationId, out _) ||
            !TryGetMutationSequence(target, mutationId, out var actualSequence) ||
            sequence.Length != actualSequence.Length ||
            complementSequence.Length != actualSequence.Length ||
            sequence.Any(nucleotide => char.ToUpperInvariant(nucleotide) is not ('A' or 'T' or 'G' or 'C')) ||
            complementSequence.Any(nucleotide => char.ToUpperInvariant(nucleotide) is not ('A' or 'T' or 'G' or 'C')) ||
            !string.Equals(sequence, actualSequence, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                complementSequence,
                new string(actualSequence.Select(GeneticMutationPrototype.GetComplement).ToArray()),
                StringComparison.OrdinalIgnoreCase))
            return false;

        var component = EnsureGenome(target);
        if (component.DiscoveredMutations.Contains(mutationId) ||
            component.ActiveMutations.Contains(mutationId))
            return false;

        var positions = new HashSet<int>();
        for (var i = 0; i < actualSequence.Length; i++)
            positions.Add(i);
        component.KnownPositions[mutationId] = positions;
        component.HalfKnownPositions.Remove(mutationId);
        component.DiscoveredMutations.Add(mutationId);
        component.SequenceGuesses[mutationId] = actualSequence;
        component.ComplementSequenceGuesses[mutationId] =
            new string(actualSequence.Select(GeneticMutationPrototype.GetComplement).ToArray());
        Dirty(target, component);
        return true;
    }

    public int GetInstability(EntityUid target)
    {
        if (!TryComp<GeneticMutationComponent>(target, out var component))
            return 0;

        var instability = 0;
        foreach (var mutationId in component.ActiveMutations)
        {
            if (_prototypeManager.TryIndex<GeneticMutationPrototype>(mutationId, out var prototype))
                instability += prototype.Instability;
        }

        return instability;
    }

    public bool TryAnalyzeMutation(EntityUid target, string mutationId)
    {
        if (!_prototypeManager.TryIndex<GeneticMutationPrototype>(mutationId, out _) ||
            !TryGetMutationSequence(target, mutationId, out var sequence))
            return false;

        var component = EnsureGenome(target);
        if (component.DiscoveredMutations.Contains(mutationId) ||
            component.ActiveMutations.Contains(mutationId))
            return false;

        component.KnownPositions.TryGetValue(mutationId, out var positions);
        positions ??= new HashSet<int>();
        component.HalfKnownPositions.TryGetValue(mutationId, out var halfKnown);
        halfKnown ??= new HashSet<int>();

        var unknown = new List<int>();
        for (var i = 0; i < sequence.Length; i++)
        {
            if (!positions.Contains(i) && !halfKnown.Contains(i))
                unknown.Add(i);
        }

        if (unknown.Count > 0)
        {
            var revealed = unknown[_robustRandom.Next(unknown.Count)];
            halfKnown.Add(revealed);
        }
        else
        {
            var partial = halfKnown.ToList();
            if (partial.Count == 0)
                return false;

            var revealed = partial[_robustRandom.Next(partial.Count)];
            halfKnown.Remove(revealed);
            positions.Add(revealed);
        }

        component.KnownPositions[mutationId] = positions;
        component.HalfKnownPositions[mutationId] = halfKnown;

        Dirty(target, component);
        return true;
    }

    public void ScrambleGenome(EntityUid target)
    {
        var component = EnsureGenome(target);
        foreach (var activeMutation in component.ActiveMutations.ToArray())
            TryRemoveMutation(target, activeMutation);

        component.DiscoveredMutations.Clear();
        component.PotentialMutations.Clear();
        component.MutationSequences.Clear();
        component.SequenceGuesses.Clear();
        component.ComplementSequenceGuesses.Clear();
        component.KnownPositions.Clear();
        component.HalfKnownPositions.Clear();
        EnsureGenome(target);
        Dirty(target, component);
    }

    public GeneticMutationComponent EnsureGenome(EntityUid target)
    {
        var component = EnsureComp<GeneticMutationComponent>(target);
        var changed = false;

        if (component.PotentialMutations.Count == 0)
        {
            var selected = component.ActiveMutations
                .Concat(component.DiscoveredMutations)
                .Distinct()
                .Where(id => _prototypeManager.TryIndex<GeneticMutationPrototype>(id, out _))
                .ToList();

            var available = _prototypeManager.EnumeratePrototypes<GeneticMutationPrototype>()
                .Select(prototype => prototype.ID)
                .Except(selected)
                .ToList();
            _robustRandom.Shuffle(available);
            selected.AddRange(available.Take(Math.Max(0, MaxPotentialMutations - selected.Count)));
            component.PotentialMutations = selected;
            changed = true;
        }

        foreach (var mutationId in component.PotentialMutations.ToArray())
        {
            if (!_prototypeManager.TryIndex<GeneticMutationPrototype>(mutationId, out var prototype))
                continue;

            if (!component.MutationSequences.ContainsKey(mutationId))
            {
                component.MutationSequences[mutationId] = GenerateMutationSequence(
                    prototype.Sequence.Length,
                    component.MutationSequences.Values);
                changed = true;
            }

            if (!component.KnownPositions.ContainsKey(mutationId) &&
                !component.HalfKnownPositions.ContainsKey(mutationId))
            {
                SeedKnownSequencePositions(component, mutationId, prototype.Sequence.Length);
                changed = true;
            }
        }

        if (changed)
            Dirty(target, component);

        return component;
    }

    public bool IsPotentialMutation(EntityUid target, string mutationId)
    {
        return EnsureGenome(target).PotentialMutations.Contains(mutationId);
    }

    public HashSet<int> GetKnownPositions(EntityUid target, string mutationId)
    {
        var component = EnsureGenome(target);
        if (component.DiscoveredMutations.Contains(mutationId) &&
            component.MutationSequences.TryGetValue(mutationId, out var seq))
        {
            var all = new HashSet<int>();
            for (var i = 0; i < seq.Length; i++)
                all.Add(i);
            return all;
        }

        return component.KnownPositions.TryGetValue(mutationId, out var positions)
            ? positions
            : new HashSet<int>();
    }

    public bool TryGetMutationSequence(EntityUid target, string mutationId, out string sequence)
    {
        var component = EnsureGenome(target);
        if (component.MutationSequences.TryGetValue(mutationId, out var actualSequence))
        {
            sequence = actualSequence;
            return true;
        }

        sequence = string.Empty;
        return false;
    }

    private string GenerateMutationSequence(int length, IEnumerable<string> existingSequences)
    {
        if (length <= 0)
            return string.Empty;

        var existing = existingSequences.ToHashSet();
        var sequence = new char[length];
        string value;
        do
        {
            for (var i = 0; i < sequence.Length; i++)
                sequence[i] = Nucleotides[_robustRandom.Next(Nucleotides.Length)];

            value = new string(sequence);
        } while (existing.Contains(value));

        return value;
    }

    private void SeedKnownSequencePositions(GeneticMutationComponent component, string mutationId, int sequenceLength)
    {
        var positions = Enumerable.Range(0, sequenceLength).ToList();
        _robustRandom.Shuffle(positions);
        var unknownCount = _robustRandom.Next(1, Math.Min(3, sequenceLength) + 1);
        var partialCount = _robustRandom.Next(sequenceLength / 2 + 1, sequenceLength - unknownCount + 1);
        var fullyKnownCount = sequenceLength - unknownCount - partialCount;
        component.KnownPositions[mutationId] = positions.Take(fullyKnownCount).ToHashSet();
        component.HalfKnownPositions[mutationId] = positions
            .Skip(fullyKnownCount)
            .Take(partialCount)
            .ToHashSet();
    }

    private void OnRefreshMovementSpeedModifiers(Entity<GeneticMutationComponent> entity, ref RefreshMovementSpeedModifiersEvent args)
    {
        foreach (var mutationId in entity.Comp.ActiveMutations)
        {
            if (!_prototypeManager.TryIndex<GeneticMutationPrototype>(mutationId, out var prototype))
                continue;

            args.ModifySpeed(prototype.WalkSpeedModifier, prototype.SprintSpeedModifier);

            if (prototype.ScaleModifier > 0f)
            {
                if (prototype.ScaleModifier < 1f)
                    args.ModifySpeed(1.1f, 1.1f);
                else if (prototype.ScaleModifier > 1f)
                    args.ModifySpeed(0.9f, 0.9f);
            }
        }
    }
}

[ByRefEvent]
public record struct GeneticMutationAppliedEvent(EntityUid Target, string MutationId);

[ByRefEvent]
public record struct GeneticMutationRemovedEvent(EntityUid Target, string MutationId);
