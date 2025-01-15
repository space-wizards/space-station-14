using System.Collections.Generic;
using System.IO;
using System.Linq;
using Content.IntegrationTests;
<<<<<<<< HEAD:Content.IntegrationTests/Tests/Impstation/Spelfs/MoodTests.cs
using Content.Server.Impstation.Spelfs;
using Content.Shared.Dataset;
using Content.Shared.Impstation.Spelfs;
========
using Content.Server._Impstation.Thaven;
using Content.Shared.Dataset;
using Content.Shared._Impstation.Thaven;
>>>>>>>> 3167d35376 (Merge pull request #1302 from hivehum/spelf-to-thaven):Content.IntegrationTests/Tests/_Impstation/Thaven/MoodTests.cs
using NUnit.Framework;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

<<<<<<<< HEAD:Content.IntegrationTests/Tests/Impstation/Spelfs/MoodTests.cs
namespace Content.IntegrationTests.Tests.Impstation.Spelfs;
========
namespace Content.IntegrationTests.Tests._Impstation.Thaven;
>>>>>>>> 3167d35376 (Merge pull request #1302 from hivehum/spelf-to-thaven):Content.IntegrationTests/Tests/_Impstation/Thaven/MoodTests.cs

[TestFixture, TestOf(typeof(ThavenMoodPrototype))]
public sealed class ThavenMoodTests
{
    [TestPrototypes]
    const string PROTOTYPES = @"
- type: dataset
  id: ThreeValueSet
  values:
    - One
    - Two
    - Three
- type: thavenMood
  id: DuplicateTest
  moodName: DuplicateTest
  moodDesc: DuplicateTest
  allowDuplicateMoodVars: false
  moodVars:
    a: ThreeValueSet
    b: ThreeValueSet
    c: ThreeValueSet
- type: thavenMood
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
        var thavenSystem = entMan.System<ThavenMoodsSystem>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();

        var dataset = protoMan.Index<DatasetPrototype>("ThreeValueSet");
        var moodProto = protoMan.Index<ThavenMoodPrototype>("DuplicateTest");

        var datasetSet = dataset.Values.ToHashSet();
        var mood = thavenSystem.RollMood(moodProto);
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
        var thavenSystem = entMan.System<ThavenMoodsSystem>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();

        var dataset = protoMan.Index<DatasetPrototype>("ThreeValueSet");
        var moodProto = protoMan.Index<ThavenMoodPrototype>("DuplicateOverlapTest");

        var datasetSet = dataset.Values.ToHashSet();
        var mood = thavenSystem.RollMood(moodProto);
        var moodVarSet = mood.MoodVars.Values.ToHashSet();

        Assert.That(moodVarSet, Is.EquivalentTo(datasetSet));

        await pair.CleanReturnAsync();
    }
}
