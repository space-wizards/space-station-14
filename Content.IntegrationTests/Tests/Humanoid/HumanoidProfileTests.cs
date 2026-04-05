using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Utility;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Speech.Components;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Humanoid;

[TestFixture]
[TestOf(typeof(HumanoidProfileSystem))]
public sealed class HumanoidProfileTests : GameTest
{
    private static readonly EntProtoId BaseSpecies = "MobHuman";
    private static readonly ProtoId<SpeciesPrototype> Vox = "Vox";
    private static string[] _species = GameDataScrounger.PrototypesOfKind<SpeciesPrototype>();

    private BodySystem _bodySystem;
    private HumanoidProfileSystem _humanoidProfile;
    private MarkingManager _markingManager;
    private SharedVisualBodySystem _visualBody;

    [Test]
    public async Task EnsureValidLoading()
    {
        var pair = Pair;
        var server = pair.Server;

        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entityManager = server.ResolveDependency<IEntityManager>();
            var humanoidProfile = entityManager.System<HumanoidProfileSystem>();
            var human = entityManager.Spawn(BaseSpecies);
            humanoidProfile.ApplyProfileTo(human,
                new HumanoidCharacterProfile()
                .WithSex(Sex.Female)
                .WithAge(67)
                .WithGender(Gender.Neuter)
                .WithSpecies(Vox));
            var humanoidComponent = entityManager.GetComponent<HumanoidProfileComponent>(human);
            var voiceComponent = entityManager.GetComponent<VocalComponent>(human);

            Assert.That(humanoidComponent.Age, Is.EqualTo(67));
            Assert.That(humanoidComponent.Sex, Is.EqualTo(Sex.Female));
            Assert.That(humanoidComponent.Gender, Is.EqualTo(Gender.Neuter));
            Assert.That(humanoidComponent.Species, Is.EqualTo(Vox));

            Assert.That(voiceComponent.Sounds, Is.Not.Null, message: "the MobHuman spawned by this test needs to have sex-specific sound set");
            Assert.That(voiceComponent.Sounds![Sex.Female], Is.EqualTo(voiceComponent.EmoteSounds));
        });
    }

    [Test]
    [TestOf(typeof(HumanoidCharacterProfile)), TestOf(typeof(VisualBodyComponent))]
    [Description("Tests that the game can generate a completely random profile with a completely random species and apply it to a blank body.")]
    public async Task EnsureValidRandom()
    {
        var pair = Pair;
        var server = pair.Server;

        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            LoadDependencies(out var body, out var humanoidComponent);
            var profile = HumanoidCharacterProfile.Random();
            _humanoidProfile.ApplyProfileTo(body, profile);
            _visualBody.ApplyProfileTo(body, profile);

            AssertValidProfile((body, humanoidComponent), profile);
        });
    }

    [Test]
    [TestOf(typeof(HumanoidCharacterProfile)), TestOf(typeof(VisualBodyComponent))]
    [TestCaseSource(nameof(_species))]
    [Description("Tests that every species is able to randomly generate a valid appearance without issues.")]
    public async Task EnsureValidRandomSpecies(string species)
    {
        await Server.WaitIdleAsync();

        await Server.WaitAssertion(() =>
        {
            LoadDependencies(out var body, out var humanoidComponent);

            var proto = Server.ProtoMan.Index<SpeciesPrototype>(species);
            var profile = HumanoidCharacterProfile.RandomWithSpecies(species);
            _humanoidProfile.ApplyProfileTo(body, profile);
            _visualBody.ApplyProfileTo(body, profile);

            Assert.That(humanoidComponent.Age, Is.LessThanOrEqualTo(proto.MaxAge));
            Assert.That(humanoidComponent.Age, Is.GreaterThanOrEqualTo(proto.MinAge));
            Assert.That(proto.Sexes.Contains(humanoidComponent.Sex), Is.True);
            Assert.That(humanoidComponent.Species, Is.EqualTo(species));
            var strategy = Server.ProtoMan.Index(proto.SkinColoration).Strategy;
            Assert.That(strategy.VerifySkinColor(profile.Appearance.SkinColor), Is.True);

            AssertValidProfile((body, humanoidComponent), profile);
        });
    }

    private void LoadDependencies(out EntityUid body, out HumanoidProfileComponent humanoidComponent)
    {
        var entityManager = Server.ResolveDependency<IEntityManager>();
        _humanoidProfile = entityManager.System<HumanoidProfileSystem>();
        _markingManager = Server.ResolveDependency<MarkingManager>();
        _visualBody = entityManager.System<SharedVisualBodySystem>();
        _bodySystem = entityManager.System<BodySystem>();
        body = entityManager.Spawn(BaseSpecies);
        humanoidComponent = entityManager.GetComponent<HumanoidProfileComponent>(body);
    }

    private void AssertValidProfile(Entity<HumanoidProfileComponent> body, HumanoidCharacterProfile profile)
    {
        _bodySystem.TryGetOrgansWithComponent<VisualOrganComponent>(body.Owner, out var organs);

        foreach (var (_, visualOrgan) in organs)
        {
            Assert.That(visualOrgan.Profile.Sex, Is.EqualTo(profile.Sex));
            Assert.That(visualOrgan.Profile.EyeColor, Is.EqualTo(profile.Appearance.EyeColor));
            Assert.That(visualOrgan.Profile.SkinColor, Is.EqualTo(profile.Appearance.SkinColor));
        }

        _bodySystem.TryGetOrgansWithComponent<VisualOrganMarkingsComponent>(body.Owner, out var markings);

        foreach (var (_, markingOrgan) in markings)
        {
            // Needed to avoid access restrictions
            var data = markingOrgan.MarkingData;
            var groupProto = Server.ProtoMan.Index(data.Group);
            var counts = new Dictionary<HumanoidVisualLayers, int>();
            var freeMarkings = new List<Marking>();

            foreach (var marking in markingOrgan.AppliedMarkings)
            {
                var markingProto = Server.ProtoMan.Index(marking.MarkingId);

                Assert.That(markingProto.Sprites.Count, Is.EqualTo(marking.MarkingColors.Count));
                Assert.That(_markingManager.CanBeApplied(data.Group, profile.Sex, markingProto), Is.True);
                Assert.That(data.Layers.Contains(markingProto.BodyPart), Is.True);
                if (!markingProto.ForcedColoring && groupProto.Appearances.GetValueOrDefault(markingProto.BodyPart)?.MatchSkin != true)
                    freeMarkings.Add(marking);

                if (!groupProto.Limits.TryGetValue(markingProto.BodyPart, out var limits))
                    continue;

                var count = counts.GetValueOrDefault(markingProto.BodyPart);
                Assert.That(count, Is.LessThanOrEqualTo(limits.Limit));
                counts[markingProto.BodyPart] = count + 1;
            }

            if (freeMarkings.Count == markingOrgan.AppliedMarkings.Count)
                continue;

            // Go through the whole list a second time just for the colors!
            foreach (var marking in markingOrgan.AppliedMarkings)
            {
                if (freeMarkings.Contains(marking))
                    continue;

                var markingProto = Server.ProtoMan.Index(marking.MarkingId);

                Assert.That(marking.MarkingColors,
                    Is.EqualTo(MarkingColoring.GetMarkingLayerColors(markingProto, profile.Appearance.SkinColor, profile.Appearance.EyeColor, markingOrgan.AppliedMarkings)));

                if (markingProto.SexRestriction != null)
                    Assert.That(markingProto.SexRestriction, Is.EqualTo(profile.Sex));
            }
        }
    }
}
