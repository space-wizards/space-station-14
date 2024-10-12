using System.Collections.Generic;
using System.IO;
using System.Linq;
using Content.IntegrationTests;
using Content.Server._Impstation.Spelfs;
using Content.Shared.Dataset;
using Content.Shared._Impstation.Spelfs;
using NUnit.Framework;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.IntegrationTests.Tests._Impstation.Spelfs;

[TestFixture, TestOf(typeof(SpelfMoodPrototype))]
public sealed class SpelfMoodTests
{
    [TestPrototypes]
    const string PROTOTYPES = @"
- type: dataset
  id: ThreeValueSet
  values:
    - One
    - Two
    - Three
- type: spelfMood
  id: DuplicateTest
  moodName: DuplicateTest
  moodDesc: DuplicateTest
  allowDuplicateMoodVars: false
  moodVars:
    a: ThreeValueSet
    b: ThreeValueSet
    c: ThreeValueSet
- type: spelfMood
  id: DuplicateOverlapTest
  moodName: DuplicateOverlapTest
  moodDesc: DuplicateOverlapTest
  allowDuplicateMoodVars: false
  moodVars:
    a: ThreeValueSet
    b: ThreeValueSet
    c: ThreeValueSet
    d: ThreeValueSet
    e: ThreeValueSet
";

    [Test]
    [Repeat(10)]
    public async Task TestDuplicatePrevention()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.ResolveDependency<IEntityManager>();
        var spelfSystem = entMan.System<SpelfMoodsSystem>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();

        var dataset = protoMan.Index<DatasetPrototype>("ThreeValueSet");
        var moodProto = protoMan.Index<SpelfMoodPrototype>("DuplicateTest");

        var datasetSet = dataset.Values.ToHashSet();
        var mood = spelfSystem.RollMood(moodProto);
        var moodVarSet = mood.MoodVars.Values.ToHashSet();

        Assert.That(moodVarSet, Is.EquivalentTo(datasetSet));

        await pair.CleanReturnAsync();
    }

    [Test]
    [Repeat(10)]
    public async Task TestDuplicateOverlap()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.ResolveDependency<IEntityManager>();
        var spelfSystem = entMan.System<SpelfMoodsSystem>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();

        var dataset = protoMan.Index<DatasetPrototype>("ThreeValueSet");
        var moodProto = protoMan.Index<SpelfMoodPrototype>("DuplicateOverlapTest");

        var datasetSet = dataset.Values.ToHashSet();
        var mood = spelfSystem.RollMood(moodProto);
        var moodVarSet = mood.MoodVars.Values.ToHashSet();

        Assert.That(moodVarSet, Is.EquivalentTo(datasetSet));

        await pair.CleanReturnAsync();
    }
}
