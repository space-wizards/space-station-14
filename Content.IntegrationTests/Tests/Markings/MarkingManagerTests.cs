#nullable enable
using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Markings;

[TestOf(typeof(MarkingManager))]
public sealed class MarkingManagerTests : GameTest
{
    private const string TestingGroup = "Testing";
    private const string TestingOtherGroup = "TestingOther";
    private const string TestingOptionalEyesGroup = "TestingOptionalEyes";
    private const string TestingRequiredEyesGroup = "TestingRequiredEyes";
    private const string SingleColorMarking = "SingleColorMarking";
    private const string MenOnlyMarking = "MenOnlyMarking";
    private const string TestingOnlyMarking = "TestingOnlyMarking";
    private const string TestingMenOnlyMarking = "TestingMenOnlyMarking";
    private const string EyesMarking = "EyesMarking";
    private const string ChestMarking = "ChestMarking";

    private static readonly ProtoId<SpeciesPrototype> HairTestSpecies = "Human";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: markingsGroup
  id: {TestingGroup}

- type: markingsGroup
  id: {TestingOtherGroup}

- type: markingsGroup
  id: {TestingOptionalEyesGroup}
  limits:
    enum.HumanoidVisualLayers.Eyes:
      limit: 1
      required: false

- type: markingsGroup
  id: {TestingRequiredEyesGroup}
  limits:
    enum.HumanoidVisualLayers.Eyes:
      limit: 1
      required: true
      default: [ {EyesMarking} ]

- type: marking
  id: {SingleColorMarking}
  bodyPart: Eyes
  sprites: [{{ sprite: Mobs/Customization/human_hair.rsi, state: afro }}]
  coloring:
    default:
      type:
        !type:EyeColoring

- type: marking
  id: {MenOnlyMarking}
  bodyPart: Eyes
  sexRestriction: Male
  sprites: [{{ sprite: Mobs/Customization/human_hair.rsi, state: afro }}]

- type: marking
  id: {TestingOnlyMarking}
  bodyPart: Eyes
  groupWhitelist: [ {TestingGroup} ]
  sprites: [{{ sprite: Mobs/Customization/human_hair.rsi, state: afro }}]

- type: marking
  id: {TestingMenOnlyMarking}
  bodyPart: Eyes
  sexRestriction: Male
  groupWhitelist: [ {TestingGroup} ]
  sprites: [{{ sprite: Mobs/Customization/human_hair.rsi, state: afro }}]

- type: marking
  id: {EyesMarking}
  bodyPart: Eyes
  sprites: [{{ sprite: Mobs/Customization/human_hair.rsi, state: afro }}]

- type: marking
  id: {ChestMarking}
  bodyPart: Chest
  sprites: [{{ sprite: Mobs/Customization/human_hair.rsi, state: afro }}]
";

    [SidedDependency(Side.Server)] private MarkingManager _sMarkingManager = null!;

    [Test]
    [RunOnSide(Side.Server)]
    public async Task HairConversion()
    {
        var markings = new List<Marking>() { new("HumanHairLongBedhead2", [Color.Red]) };

        var converted = _sMarkingManager.ConvertMarkings(markings, HairTestSpecies);

        Assert.That(converted, Does.ContainKey(new ProtoId<OrganCategoryPrototype>("Head")));
        Assert.That(converted["Head"], Does.ContainKey(HumanoidVisualLayers.Hair));
        var hairMarkings = converted["Head"][HumanoidVisualLayers.Hair];
        Assert.That(hairMarkings, Has.Count.EqualTo(1));
        Assert.That(hairMarkings[0].MarkingId, Is.EqualTo("HumanHairLongBedhead2"));
        Assert.That(hairMarkings[0].MarkingColors[0], Is.EqualTo(Color.Red));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public async Task LimitsFilling()
    {
        var dict = new Dictionary<HumanoidVisualLayers, List<Marking>>();

        _sMarkingManager.EnsureValidLimits(dict, TestingRequiredEyesGroup, [HumanoidVisualLayers.Eyes], null, null);
        Assert.That(dict, Does.ContainKey(HumanoidVisualLayers.Eyes));
        Assert.That(dict[HumanoidVisualLayers.Eyes], Has.Count.EqualTo(1));
        Assert.That(dict[HumanoidVisualLayers.Eyes][0].MarkingId, Is.EqualTo(EyesMarking));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public async Task LimitsTruncations()
    {
        var dict = new Dictionary<HumanoidVisualLayers, List<Marking>>()
        {
            [HumanoidVisualLayers.Eyes] =
            [
                new(EyesMarking, 0),
                new(MenOnlyMarking, 0),
            ],
        };

        _sMarkingManager.EnsureValidLimits(dict, TestingOptionalEyesGroup, [HumanoidVisualLayers.Eyes], null, null);
        Assert.That(dict[HumanoidVisualLayers.Eyes], Has.Count.EqualTo(1));
        Assert.That(dict[HumanoidVisualLayers.Eyes][0].MarkingId, Is.EqualTo(MenOnlyMarking));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public async Task EnsureValidGroupAndSex()
    {
        static Dictionary<HumanoidVisualLayers, List<Marking>> DictFactory() => new()
        {
            [HumanoidVisualLayers.Eyes] =
            [
                new(MenOnlyMarking, 0),
                new(TestingOnlyMarking, 0),
                new(TestingMenOnlyMarking, 0),
            ]
        };

        var menMarkings = DictFactory();
        _sMarkingManager.EnsureValidGroupAndSex(menMarkings, TestingOtherGroup, Sex.Male);

        Assert.That(menMarkings[HumanoidVisualLayers.Eyes], Has.Count.EqualTo(1));
        Assert.That(menMarkings[HumanoidVisualLayers.Eyes][0].MarkingId, Is.EqualTo(MenOnlyMarking));

        var testingMarkings = DictFactory();
        _sMarkingManager.EnsureValidGroupAndSex(testingMarkings, TestingGroup, Sex.Female);

        Assert.That(testingMarkings[HumanoidVisualLayers.Eyes], Has.Count.EqualTo(1));
        Assert.That(testingMarkings[HumanoidVisualLayers.Eyes][0].MarkingId, Is.EqualTo(TestingOnlyMarking));

        var testingMenMarkings = DictFactory();
        _sMarkingManager.EnsureValidGroupAndSex(testingMenMarkings, TestingGroup, Sex.Male);

        Assert.That(testingMenMarkings[HumanoidVisualLayers.Eyes], Has.Count.EqualTo(3));
        Assert.That(testingMenMarkings[HumanoidVisualLayers.Eyes][0].MarkingId, Is.EqualTo(MenOnlyMarking));
        Assert.That(testingMenMarkings[HumanoidVisualLayers.Eyes][1].MarkingId, Is.EqualTo(TestingOnlyMarking));
        Assert.That(testingMenMarkings[HumanoidVisualLayers.Eyes][2].MarkingId, Is.EqualTo(TestingMenOnlyMarking));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public async Task EnsureValidColors()
    {
        var dict = new Dictionary<HumanoidVisualLayers, List<Marking>>()
        {
            [HumanoidVisualLayers.Eyes] =
            [
                new(SingleColorMarking, 0),
                new(SingleColorMarking, [Color.Red]),
                new(SingleColorMarking, 2),
                new(SingleColorMarking, [Color.Green]),
            ]
        };

        _sMarkingManager.EnsureValidColors(dict);

        var eyeMarkings = dict[HumanoidVisualLayers.Eyes];

        using (Assert.EnterMultipleScope())
        {
            // ensure all colors are the correct length
            Assert.That(eyeMarkings[0].MarkingColors, Has.Count.EqualTo(1));
            Assert.That(eyeMarkings[1].MarkingColors, Has.Count.EqualTo(1));
            Assert.That(eyeMarkings[2].MarkingColors, Has.Count.EqualTo(1));
            Assert.That(eyeMarkings[3].MarkingColors, Has.Count.EqualTo(1));

            // and make sure we didn't shuffle our colors around
            Assert.That(eyeMarkings[1].MarkingColors[0], Is.EqualTo(Color.Red));
            Assert.That(eyeMarkings[3].MarkingColors[0], Is.EqualTo(Color.Green));
        }
    }
}
