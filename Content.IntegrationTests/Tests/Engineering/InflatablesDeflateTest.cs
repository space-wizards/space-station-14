using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Engineering.Systems;

namespace Content.IntegrationTests.Tests.Engineering;

[TestFixture]
[TestOf(typeof(InflatableSafeDisassemblySystem))]
public sealed class InflatablesDeflateTest : InteractionTest
{
    [Test]
    public async Task Test()
    {
        await SpawnTarget(InflatableWall);

        await InteractUsing(Needle);

        AssertDeleted();
        await AssertEntityLookup(new EntitySpecifier(InflatableWallStack.Id, 1));
    }
}
