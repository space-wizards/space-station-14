using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Chemistry;

[TestFixture]
[TestOf(typeof(ReagentData))]
public sealed class ReagentDataTest : InteractionTest
{
    [Test]
    public async Task ReagentDataIsSerializable()
    {
        await using var pair = await PoolManager.GetServerClient();
        var reflection = pair.Server.ResolveDependency<IReflectionManager>();

        Assert.Multiple(() =>
        {
            foreach (var instance in reflection.GetAllChildren(typeof(ReagentData)))
            {
                Assert.That(instance.HasCustomAttribute<NetSerializableAttribute>(), $"{instance} must have the NetSerializable attribute.");
                Assert.That(instance.HasCustomAttribute<SerializableAttribute>(), $"{instance} must have the serializable attribute.");
            }
        });

        await pair.CleanReturnAsync();
    }
}
