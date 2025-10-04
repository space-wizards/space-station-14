using Content.Shared.Atmos.Components;

namespace Content.IntegrationTests.Tests.Atmos;

[TestFixture]
public sealed class PipeLayerTest
{
    [Test]
    public async Task TestPipeLayersValid()
    {
        await using var pair = await PoolManager.GetServerClient();
        var layerCount = Enum.GetValues<AtmosPipeLayer>().Length;

        Assert.Multiple(() =>
        {
            foreach (var (proto, comp) in pair.GetPrototypesWithComponent<AtmosPipeLayersComponent>())
            {
                Assert.That(comp.NumberOfPipeLayers,
                    Is.AtMost(layerCount),
                    $"Prototype {proto.ID} has a higher number of pipe layers than supported.");
            }
        });

        await pair.CleanReturnAsync();
    }
}
