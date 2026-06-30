using System.Linq;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.EntityTable.ValueSelector;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;

namespace Content.Tests.Shared;

[TestFixture]
[TestOf(typeof(EntityTableSelector))]
[TestOf(typeof(AllSelector))]
[TestOf(typeof(EntSelector))]
[TestOf(typeof(GroupSelector))]
[TestOf(typeof(NestedSelector))]
[TestOf(typeof(NoneSelector))]
public sealed partial class EntityTableSelectorTest : ContentUnitTest
{
    private static RobustRandom Rand(int seed = 0)
    {
        var ret = new RobustRandom();
        ret.SetSeed(seed);
        return ret;
    }

    private EntityTableSystem _entTable = default!;

    private const string A = "A";
    private const string B = "B";

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        IoCManager.Resolve<ISerializationManager>().Initialize();

        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        protoMan.Initialize();
        protoMan.LoadString(NestedPrototypes);
        protoMan.ResolveResults();

        _entTable = IoCManager.InjectDependencies(new EntityTableSystem());
    }

    [Test]
    public void AllSelectorTest()
    {
        var s = new AllSelector
        {
            Prob = 1f,
            Rolls = new ConstantNumberSelector(2),
            Children =
            [
                new EntSelector { Id = A },
                new EntSelector { Id = B },
            ],
        };
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_entTable.GetSpawns(s, Rand()), Is.EquivalentTo([A, B, A, B]));
            Assert.That(_entTable.ListSpawns(s), Is.EquivalentTo([(A, 1f), (B, 1f)]));
            Assert.That(_entTable.AverageSpawns(s), Is.EquivalentTo([(A, 2f), (B, 2f)]));
        }
    }

    [Test]
    public void EntSelectorTest()
    {
        var s = new EntSelector
        {
            Amount = new ConstantNumberSelector(2),
            Id = A,
            Prob = 1f,
            Rolls = new ConstantNumberSelector(3),
        };
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_entTable.GetSpawns(s, Rand()), Is.EquivalentTo(Enumerable.Repeat(A, 6)));
            Assert.That(_entTable.ListSpawns(s), Is.EquivalentTo([(A, 1f)]));
            Assert.That(_entTable.AverageSpawns(s), Is.EquivalentTo([(A, 6f)]));
        }
    }

    [Test]
    public void GroupSelectorTest()
    {
        var s = new GroupSelector
        {
            Prob = 1f,
            Rolls = new ConstantNumberSelector(4),
            Children =
            [
                new EntSelector { Id = A, Weight = 3f },
                new EntSelector { Id = B, Weight = 1f },
            ],
        };
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_entTable.GetSpawns(s, Rand()).ToList(), Has.Count.EqualTo(4));
            Assert.That(_entTable.ListSpawns(s), Is.EquivalentTo([(A, 0.75f), (B, 0.25f)]));
            Assert.That(_entTable.AverageSpawns(s), Is.EquivalentTo([(A, 3f), (B, 1f)]));
        }
    }

    private const string TestTable = "TestTable";

    private const string NestedPrototypes = $@"
- type: entityTable
  id: {TestTable}
  table:
    !type:EntSelector
    id: {A}
";

    [Test]
    public void NestedSelectorTest()
    {
        var s = new NestedSelector
        {
            TableId = TestTable,
            Prob = 1f,
            Rolls = new ConstantNumberSelector(1),
        };
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_entTable.GetSpawns(s, Rand()), Is.EquivalentTo([A]));
            Assert.That(_entTable.ListSpawns(s), Is.EquivalentTo([(A, 1f)]));
            Assert.That(_entTable.AverageSpawns(s), Is.EquivalentTo([(A, 1f)]));
        }
    }

    [Test]
    public void NoneSelectorTest()
    {
        var s = new NoneSelector();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_entTable.GetSpawns(s, Rand()), Is.Empty);
            Assert.That(_entTable.ListSpawns(s), Is.Empty);
            Assert.That(_entTable.AverageSpawns(s), Is.Empty);
        }
    }
}
