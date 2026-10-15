using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.Genetics;
using Content.Server.Polymorph.Systems;
using Content.Shared.Body;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Genetics;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Overlays;
using Content.Shared.Polymorph;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Genetics;

[TestFixture]
public sealed class GeneticsMutationTest : GameTest
{
    private static readonly ProtoId<GeneticMutationPrototype> AdaptiveVisionMutation = "GeneticMutationAdaptiveVision";
    private static readonly ProtoId<PolymorphPrototype> GeneticsMonkeyToHuman = "GeneticsMonkeyToHuman";

    [Test]
    public async Task ConsoleHasTenMemorySlotsAndMonkeyCanBecomeHuman()
    {
        await Server.WaitAssertion(() =>
        {
            Assert.That(new GeneticsConsoleComponent().MaxMemorySlots, Is.EqualTo(10));

            var polymorph = Server.ProtoMan.Index(GeneticsMonkeyToHuman).Configuration;
            Assert.That(polymorph.Entity, Is.EqualTo("MobHuman"));
            Assert.That(polymorph.TransferDamage, Is.True);
            Assert.That(polymorph.Inventory, Is.EqualTo(PolymorphInventoryChange.Transfer));
            Assert.That(polymorph.RevertOnDeath, Is.False);

            var monkey = SSpawn("MobMonkey");
            var human = Server.System<PolymorphSystem>().PolymorphEntity(monkey, GeneticsMonkeyToHuman);
            Assert.That(human, Is.Not.Null);
            Assert.That(SEntMan.HasComponent<HumanoidProfileComponent>(human!.Value), Is.True);
        });
    }

    [Test]
    public async Task MutationSequencesContainSixteenValidBasePairs()
    {
        await Server.WaitAssertion(() =>
        {
            foreach (var mutation in Server.ProtoMan.EnumeratePrototypes<GeneticMutationPrototype>())
            {
                Assert.That(mutation.Sequence.Length, Is.EqualTo(16));
                Assert.That(mutation.Sequence.All(nucleotide => nucleotide is 'A' or 'T' or 'G' or 'C'), Is.True);
            }

            Assert.That(GeneticMutationPrototype.GetComplement('A'), Is.EqualTo('T'));
            Assert.That(GeneticMutationPrototype.GetComplement('T'), Is.EqualTo('A'));
            Assert.That(GeneticMutationPrototype.GetComplement('G'), Is.EqualTo('C'));
            Assert.That(GeneticMutationPrototype.GetComplement('C'), Is.EqualTo('G'));
        });
    }

    [Test]
    public async Task StandardTgMutationCatalogIsAvailable()
    {
        await Server.WaitAssertion(() =>
        {
            var mutations = Server.ProtoMan
                .EnumeratePrototypes<GeneticMutationPrototype>()
                .Select(mutation => mutation.ID.ToString())
                .ToHashSet();
            var expected = new[]
            {
                "GeneticMutationAcidFlesh",
                "GeneticMutationAntenna",
                "GeneticMutationAntiGlow",
                "GeneticMutationBlindness",
                "GeneticMutationChameleon",
                "GeneticMutationClumsiness",
                "GeneticMutationCough",
                "GeneticMutationCryokinesis",
                "GeneticMutationDeafness",
                "GeneticMutationSizeSmall",
                "GeneticMutationElvis",
                "GeneticMutationEpilepsy",
                "GeneticMutationGeladikinesis",
                "GeneticMutationSizeLarge",
                "GeneticMutationNearsighted",
                "GeneticMutationBioluminescence",
                "GeneticMutationHulk",
                "GeneticMutationIlliteracy",
                "GeneticMutationInsulated",
                "GeneticMutationLaserEyes",
                "GeneticMutationMindreader",
                "GeneticMutationMute",
                "GeneticMutationOlfaction",
                "GeneticMutationPigLatin",
                "GeneticMutationParanoia",
                "GeneticMutationPressureAdaptation",
                "GeneticMutationRadioactive",
                "GeneticMutationShockTouch",
                "GeneticMutationSpastic",
                "GeneticMutationSpatialInstability",
                "GeneticMutationStuttering",
                "GeneticMutationSwedish",
                "GeneticMutationTelekinesis",
                "GeneticMutationTemperatureAdaptation",
                "GeneticMutationThermalVision",
                "GeneticMutationTourette",
                "GeneticMutationTwoLeftFeet",
                "GeneticMutationUnintelligible",
                "GeneticMutationVoid",
                "GeneticMutationXRay",
                "GeneticMutationWacky",
                "GeneticMutationWebbing",
                "GeneticMutationClever",
            };

            Assert.That(mutations, Is.SupersetOf(expected));
        });
    }

    [Test]
    public async Task EachSubjectGetsRandomPotentialMutationsAndGeneSequences()
    {
        EntityUid firstSubject = default;
        EntityUid secondSubject = default;
        await Server.WaitAssertion(() =>
        {
            firstSubject = SSpawn("MobHuman");
            secondSubject = SSpawn("MobHuman");
        });

        await Server.WaitAssertion(() =>
        {
            var genetics = Server.System<SharedGeneticsSystem>();
            var prototypes = Server.ProtoMan.EnumeratePrototypes<GeneticMutationPrototype>().ToList();
            var firstGenome = genetics.EnsureGenome(firstSubject);
            var secondGenome = genetics.EnsureGenome(secondSubject);

            Assert.That(firstGenome.PotentialMutations.Count,
                Is.EqualTo(Math.Min(SharedGeneticsSystem.MaxPotentialMutations, prototypes.Count)));
            Assert.That(firstGenome.PotentialMutations.Distinct().Count(),
                Is.EqualTo(firstGenome.PotentialMutations.Count));
            Assert.That(firstGenome.MutationSequences.Values.Distinct().Count(),
                Is.EqualTo(firstGenome.MutationSequences.Count));

            var sharedMutations = firstGenome.PotentialMutations
                .Intersect(secondGenome.PotentialMutations)
                .ToList();
            Assert.That(sharedMutations, Is.Not.Empty);
            Assert.That(sharedMutations.Any(mutationId =>
                    firstGenome.MutationSequences[mutationId] != secondGenome.MutationSequences[mutationId]),
                Is.True);
        });
    }

    [Test]
    public async Task MutationSequenceMustBeGuessedBeforeApplication()
    {
        EntityUid subject = default;
        await Server.WaitAssertion(() => subject = SSpawn("MobHuman"));

        var genetics = Server.System<SharedGeneticsSystem>();

        await Server.WaitAssertion(() =>
        {
            var mutationId = genetics.EnsureGenome(subject).PotentialMutations[0];
            var mutation = Server.ProtoMan.Index<GeneticMutationPrototype>(mutationId);
            var sequence = genetics.EnsureGenome(subject).MutationSequences[mutation.ID];
            var incorrectSequence = sequence.ToCharArray();
            incorrectSequence[0] = incorrectSequence[0] == 'A' ? 'T' : 'A';

            Assert.That(mutation.Sequence.Length, Is.EqualTo(16));
            Assert.That(genetics.TryApplyMutation(subject, mutation.ID), Is.False);
            Assert.That(genetics.TryGuessMutation(subject, mutation.ID, new string(incorrectSequence)), Is.False);
            Assert.That(genetics.TryApplyMutation(subject, mutation.ID), Is.False);
            var complement = new string(sequence.Select(GeneticMutationPrototype.GetComplement).ToArray());
            var incorrectComplement = complement.ToCharArray();
            incorrectComplement[0] = incorrectComplement[0] == 'A' ? 'T' : 'A';
            Assert.That(
                genetics.TryGuessMutation(subject, mutation.ID, sequence, new string(incorrectComplement)),
                Is.False);
            Assert.That(genetics.TryGuessMutation(subject, mutation.ID, sequence, complement), Is.True);
            Assert.That(genetics.TryApplyMutation(subject, mutation.ID), Is.True);
            Assert.That(genetics.TryApplyMutation(subject, mutation.ID), Is.False);
            Assert.That(genetics.GetInstability(subject), Is.EqualTo(mutation.Instability));

            var movement = SEntMan.GetComponent<MovementSpeedModifierComponent>(subject);
            Assert.That(movement.SprintSpeedModifier, Is.EqualTo(mutation.SprintSpeedModifier).Within(0.001f));
            Assert.That(movement.WalkSpeedModifier, Is.EqualTo(mutation.WalkSpeedModifier).Within(0.001f));
        });
    }

    [Test]
    public async Task SequenceAnalysisDistinguishesKnownPartialAndUnknownPairs()
    {
        EntityUid subject = default;
        await Server.WaitAssertion(() => subject = SSpawn("MobHuman"));

        await Server.WaitAssertion(() =>
        {
            var genetics = Server.System<SharedGeneticsSystem>();
            var genome = genetics.EnsureGenome(subject);
            var mutationId = genome.PotentialMutations[0];

            var initialKnown = genome.KnownPositions[mutationId].Count;
            var initialPartial = genome.HalfKnownPositions[mutationId].Count;
            var initialUnknown = genome.MutationSequences[mutationId].Length - initialKnown - initialPartial;
            Assert.That(initialUnknown, Is.InRange(1, 3));
            Assert.That(initialPartial, Is.GreaterThan(genome.MutationSequences[mutationId].Length / 2));

            Assert.That(genetics.TryAnalyzeMutation(subject, mutationId), Is.True);
            Assert.That(genome.KnownPositions[mutationId].Count, Is.EqualTo(initialKnown));
            Assert.That(genome.HalfKnownPositions[mutationId].Count, Is.EqualTo(initialPartial + 1));

            var sequenceLength = genome.MutationSequences[mutationId].Length;
            while (genome.KnownPositions[mutationId].Count + genome.HalfKnownPositions[mutationId].Count < sequenceLength)
                Assert.That(genetics.TryAnalyzeMutation(subject, mutationId), Is.True);

            Assert.That(genetics.TryAnalyzeMutation(subject, mutationId), Is.True);
            Assert.That(genome.KnownPositions[mutationId].Count, Is.EqualTo(initialKnown + 1));
            Assert.That(genome.HalfKnownPositions[mutationId].Count, Is.EqualTo(sequenceLength - initialKnown - 1));
        });
    }

    [Test]
    public async Task ActionMutationsGrantAndRemoveTheirActions()
    {
        EntityUid subject = default;
        await Server.WaitAssertion(() => subject = SSpawn("MobHuman"));

        await Server.WaitAssertion(() =>
        {
            var genetics = Server.System<SharedGeneticsSystem>();
            _ = Server.System<GeneticMutationEffectsSystem>();
            var genome = genetics.EnsureGenome(subject);

            foreach (var actionMutationId in new[]
                     {
                         "GeneticMutationTelekinesis",
                         "GeneticMutationTelepathy",
                         "GeneticMutationShockTouch",
                         "GeneticMutationSpeciesShift",
                     })
            {
                Assert.That(genetics.TryApplyMutationFromInjector(subject, actionMutationId), Is.True);
            }

            Assert.That(genome.TelekinesisAction, Is.Not.Null);
            Assert.That(genome.TelepathyAction, Is.Not.Null);
            Assert.That(genome.ShockTouchAction, Is.Not.Null);
            Assert.That(genome.SpeciesShiftAction, Is.Not.Null);
            var shiftEvent = new GeneticSpeciesShiftActionEvent();
            SEntMan.EventBus.RaiseLocalEvent(subject, shiftEvent);
            Assert.That(shiftEvent.Handled, Is.True);
            Assert.That(genome.SpeciesShiftAction, Is.Not.Null);

            shiftEvent = new GeneticSpeciesShiftActionEvent();
            SEntMan.EventBus.RaiseLocalEvent(subject, shiftEvent);
            Assert.That(shiftEvent.Handled, Is.True);

            foreach (var actionMutationId in new[]
                     {
                         "GeneticMutationTelekinesis",
                         "GeneticMutationTelepathy",
                         "GeneticMutationShockTouch",
                         "GeneticMutationSpeciesShift",
                     })
            {
                Assert.That(genetics.TryRemoveMutation(subject, actionMutationId), Is.True);
            }

            Assert.That(genome.TelekinesisAction, Is.Null);
            Assert.That(genome.TelepathyAction, Is.Null);
            Assert.That(genome.ShockTouchAction, Is.Null);
            Assert.That(genome.SpeciesShiftAction, Is.Null);
        });
    }

    [Test]
    public async Task HulkMutationAddsGreenSkinAndStunImmunityThenRestoresThem()
    {
        EntityUid subject = default;
        await Server.WaitAssertion(() => subject = SSpawn("MobHuman"));

        await Server.WaitAssertion(() =>
        {
            var genetics = Server.System<SharedGeneticsSystem>();
            _ = Server.System<GeneticMutationEffectsSystem>();
            var visualBody = Server.System<SharedVisualBodySystem>();
            var tags = Server.System<TagSystem>();
            var stunImmuneTag = new ProtoId<TagPrototype>("StunImmune");

            Assert.That(visualBody.TryGatherMarkingsData(subject, null, out var originalProfiles, out _, out _), Is.True);
            Assert.That(genetics.TryApplyMutationFromInjector(subject, "GeneticMutationHulk"), Is.True);
            Assert.That(tags.HasTag(subject, stunImmuneTag), Is.True);
            Assert.That(visualBody.TryGatherMarkingsData(subject, null, out var greenProfiles, out _, out _), Is.True);
            Assert.That(greenProfiles.Values.All(profile => profile.SkinColor == Color.Green), Is.True);

            Assert.That(genetics.TryRemoveMutation(subject, "GeneticMutationHulk"), Is.True);
            Assert.That(tags.HasTag(subject, stunImmuneTag), Is.False);
            Assert.That(visualBody.TryGatherMarkingsData(subject, null, out var restoredProfiles, out _, out _), Is.True);
            Assert.That(restoredProfiles, Is.EqualTo(originalProfiles));
        });
    }

    [Test]
    public async Task MutadoneRemovesActiveMutationEffects()
    {
        EntityUid subject = default;
        await Server.WaitAssertion(() => subject = SSpawn("MobHuman"));

        await Server.WaitAssertion(() =>
        {
            var genetics = Server.System<SharedGeneticsSystem>();
            var effects = Server.System<SharedEntityEffectsSystem>();
            _ = Server.System<GeneticMutationEffectsSystem>();
            var tags = Server.System<TagSystem>();
            var stunImmuneTag = new ProtoId<TagPrototype>("StunImmune");
            var genome = genetics.EnsureGenome(subject);

            Assert.That(genetics.TryApplyMutationFromInjector(subject, "GeneticMutationHulk"), Is.True);
            Assert.That(genome.ActiveMutations, Does.Contain("GeneticMutationHulk"));
            Assert.That(tags.HasTag(subject, stunImmuneTag), Is.True);

            effects.ApplyEffects(subject, new EntityEffect[] { new RemoveAllGeneticMutations() });

            Assert.That(genome.ActiveMutations, Is.Empty);
            Assert.That(genome.MutadoneRecoveryActive, Is.True);
            Assert.That(tags.HasTag(subject, stunImmuneTag), Is.False);
        });
    }

    [Test]
    public async Task ScrambleRegeneratesSevenGeneBlocksAndClearsOldDiscovery()
    {
        EntityUid subject = default;
        await Server.WaitAssertion(() => subject = SSpawn("MobHuman"));

        await Server.WaitAssertion(() =>
        {
            var genetics = Server.System<SharedGeneticsSystem>();
            _ = Server.System<GeneticMutationEffectsSystem>();
            var genome = genetics.EnsureGenome(subject);
            var mutationId = genome.PotentialMutations[0];
            var sequence = genome.MutationSequences[mutationId];
            Assert.That(genetics.TryGuessMutation(subject, mutationId, sequence), Is.True);
            Assert.That(genome.DiscoveredMutations, Does.Contain(mutationId));

            var adaptiveVision = Server.ProtoMan.Index(AdaptiveVisionMutation);
            Assert.That(genetics.TryApplyMutationFromInjector(subject, adaptiveVision.ID), Is.True);
            Assert.That(SEntMan.HasComponent<NightVisionComponent>(subject), Is.True);

            genetics.ScrambleGenome(subject);
            genome = SEntMan.GetComponent<GeneticMutationComponent>(subject);

            Assert.That(genome.PotentialMutations.Count, Is.EqualTo(SharedGeneticsSystem.MaxPotentialMutations));
            Assert.That(genome.PotentialMutations.Distinct().Count(), Is.EqualTo(genome.PotentialMutations.Count));
            Assert.That(genome.DiscoveredMutations, Is.Empty);
            Assert.That(genome.ActiveMutations, Is.Empty);
            Assert.That(SEntMan.HasComponent<NightVisionComponent>(subject), Is.False);
            foreach (var scrambledId in genome.PotentialMutations)
            {
                var sequenceLength = genome.MutationSequences[scrambledId].Length;
                var known = genome.KnownPositions[scrambledId].Count;
                var partial = genome.HalfKnownPositions[scrambledId].Count;
                var unknown = sequenceLength - known - partial;
                Assert.That(unknown, Is.InRange(1, 3));
                Assert.That(partial, Is.GreaterThan(sequenceLength / 2));
            }
        });
    }

    [Test]
    public async Task AdaptiveVisionAddsAndRemovesNightVision()
    {
        EntityUid subject = default;
        await Server.WaitAssertion(() => subject = SSpawn("MobHuman"));

        var genetics = Server.System<SharedGeneticsSystem>();
        _ = Server.System<GeneticMutationEffectsSystem>();
        var mutation = Server.ProtoMan.Index(AdaptiveVisionMutation);

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<NightVisionComponent>(subject), Is.False);
            Assert.That(genetics.TryApplyMutationFromInjector(subject, mutation.ID), Is.True);
            Assert.That(SEntMan.HasComponent<NightVisionComponent>(subject), Is.True);
            Assert.That(genetics.TryRemoveMutation(subject, mutation.ID), Is.True);
            Assert.That(SEntMan.HasComponent<NightVisionComponent>(subject), Is.False);
        });
    }

    [Test]
    public async Task InstabilityFromActiveMutationsIsCombined()
    {
        EntityUid subject = default;
        await Server.WaitAssertion(() => subject = SSpawn("MobHuman"));

        var genetics = Server.System<SharedGeneticsSystem>();
        var mutations = Server.ProtoMan.EnumeratePrototypes<GeneticMutationPrototype>().ToList();

        await Server.WaitAssertion(() =>
        {
            foreach (var mutation in mutations)
            {
                Assert.That(genetics.TryApplyMutationFromInjector(subject, mutation.ID), Is.True);
            }

            Assert.That(genetics.GetInstability(subject), Is.GreaterThan(100));
            SEntMan.GetComponent<GeneticMutationComponent>(subject).UnstableDamageTimer = 0f;

            var instabilitySystem = Server.System<GeneticInstabilitySystem>();
            var damageable = Server.System<DamageableSystem>();
            var mobState = Server.System<MobStateSystem>();
            instabilitySystem.Update(9f);
            var damage = damageable.GetAllDamage(subject);
            damage.DamageDict.TryGetValue("Cellular", out var cellularDamage);
            Assert.That(cellularDamage, Is.EqualTo(FixedPoint2.Zero));

            instabilitySystem.Update(1f);
            damage = damageable.GetAllDamage(subject);
            Assert.That(damage.DamageDict["Cellular"], Is.EqualTo(FixedPoint2.New(15)));

            for (var pulse = 1; pulse < 7; pulse++)
                instabilitySystem.Update(10f);

            Assert.That(damageable.GetAllDamage(subject).DamageDict["Cellular"], Is.EqualTo(FixedPoint2.New(105)));
            Assert.That(mobState.IsDead(subject), Is.False);

            for (var pulse = 7; pulse < 10; pulse++)
                instabilitySystem.Update(10f);

            damage = damageable.GetAllDamage(subject);
            Assert.That(damage.DamageDict["Cellular"], Is.EqualTo(FixedPoint2.New(150)));
            Assert.That(mobState.IsDead(subject), Is.True);
        });
    }
}
