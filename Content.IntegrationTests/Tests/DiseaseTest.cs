using System.Threading.Tasks;
using Content.Shared.Disease;
using NUnit.Framework;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests;

[TestFixture]
public sealed class DiseaseTest
{
    /// <summary>
    /// Asserts that a disease prototype has valid stages for its effects and cures.
    /// </summary>
    [Test]
    public async Task Stages()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
        var server = pairTracker.Pair.Server;

        var protoManager = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            foreach (var proto in protoManager.EnumeratePrototypes<DiseasePrototype>())
            {
                var stagesLength = proto.Stages.Count;

                foreach (var effect in proto.Effects)
                {
                    for (var i = 0; i < effect.Stages.Length; i++)
                    {
                        Assert.That(i >= 0 && i < stagesLength, $"Disease {proto.ID} has an effect with an incorrect stage, {i}!");
                    }
                }

                foreach (var cure in proto.Cures)
                {
                    for (var i = 0; i < cure.Stages.Length; i++)
                    {
                        Assert.That(i >= 0 && i < stagesLength, $"Disease {proto.ID} has a cure with an incorrect stage, {i}!");
                    }
                }
            }
        });

        await pairTracker.CleanReturnAsync();
    }
}
