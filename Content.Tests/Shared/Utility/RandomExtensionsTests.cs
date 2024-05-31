using System.IO;
using Content.Shared.Dataset;
using Content.Shared.Random.Helpers;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;

namespace Content.Tests.Shared.Utility
{
    [TestFixture]
    [TestOf(typeof(SharedRandomExtensions))]
    public sealed class RandomExtensionsTests : ContentUnitTest
    {
        private const string TestDatasetId = "TestDataset";

        private static readonly string Prototypes = $@"
- type: dataset
  id: {TestDatasetId}
  values:
  - A";

        [Test]
        public void RandomDataSetValueTest()
        {
            IoCManager.Resolve<ISerializationManager>().Initialize();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.Initialize();

            prototypeManager.LoadFromStream(new StringReader(Prototypes));
            prototypeManager.ResolveResults();

            var dataSet = prototypeManager.Index<DatasetPrototype>(TestDatasetId);
            var random = IoCManager.Resolve<IRobustRandom>();
            var id = random.Pick(dataSet);

            Assert.That(id, Is.Not.Null);
        }
    }
}
