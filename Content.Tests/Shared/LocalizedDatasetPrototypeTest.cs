using System;
using Content.Shared.Dataset;
using NUnit.Framework;
using Robust.Shared.Collections;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Tests.Shared;

[TestFixture]
[TestOf(typeof(LocalizedDatasetPrototype))]
public sealed class LocalizedDatasetPrototypeTest : ContentUnitTest
{
    private IPrototypeManager _prototypeManager;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        IoCManager.Resolve<ISerializationManager>().Initialize();
        _prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        _prototypeManager.Initialize();
        _prototypeManager.LoadString(TestPrototypes);
        _prototypeManager.ResolveResults();
    }

    private const string TestPrototypes = @"
- type: localizedDataset
  id: Test
  values:
    prefix: test-dataset-
    count: 4
";

    [Test]
    public void LocalizedDatasetTest()
    {
        var testPrototype = _prototypeManager.Index<LocalizedDatasetPrototype>("Test");
        var values = new ValueList<string>();
        foreach (var value in testPrototype.Values)
        {
            values.Add(value);
        }

        // Make sure we get the right number of values
        Assert.That(values, Has.Count.EqualTo(4));

        // Make sure indexing works as expected
        Assert.That(testPrototype.Values[0], Is.EqualTo("test-dataset-1"));
        Assert.That(testPrototype.Values[1], Is.EqualTo("test-dataset-2"));
        Assert.That(testPrototype.Values[2], Is.EqualTo("test-dataset-3"));
        Assert.That(testPrototype.Values[3], Is.EqualTo("test-dataset-4"));
        Assert.Throws<IndexOutOfRangeException>(() => { var x = testPrototype.Values[4]; });
        Assert.Throws<IndexOutOfRangeException>(() => { var x = testPrototype.Values[-1]; });

        // Make sure that the enumerator gets all of the values
        Assert.That(testPrototype.Values[^1], Is.EqualTo("test-dataset-4"));
    }
}
