#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Chemistry;

[TestOf(typeof(ReagentData))]
public sealed class ReagentDataTest : GameTest
{
    [SidedDependency(Side.Server)] private IReflectionManager _sReflection = null!;

    [Test]
    public async Task ReagentDataIsSerializable()
    {
        using (Assert.EnterMultipleScope())
        {
            foreach (var instance in _sReflection.GetAllChildren<ReagentData>())
            {
                Assert.That(instance.HasCustomAttribute<NetSerializableAttribute>(), $"{instance} must have {nameof(NetSerializableAttribute)}.");
                Assert.That(instance.HasCustomAttribute<SerializableAttribute>(), $"{instance} must have {nameof(SerializableAttribute)}.");
            }
        }
    }
}
