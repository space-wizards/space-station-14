using Content.Client.Gameplay;
using Content.Client.Mapping;
using Content.IntegrationTests.Fixtures;
using Robust.Client.State;

namespace Content.IntegrationTests.Tests;

[TestFixture]
public sealed class MappingEditorTest : GameTest
{
    [Test]
    public async Task StopHardCodingWidgetsJesusChristTest()
    {
        var pair = Pair;
        var client = pair.Client;
        var state = client.ResolveDependency<IStateManager>();

        await client.WaitPost(() =>
        {
            Assert.DoesNotThrow(() =>
            {
                state.RequestStateChange<MappingState>();
            });
        });

        // arbitrary short time
        await client.WaitRunTicks(30);

        await client.WaitPost(() =>
        {
            Assert.DoesNotThrow(() =>
            {
                state.RequestStateChange<GameplayState>();
            });
        });
    }
}
