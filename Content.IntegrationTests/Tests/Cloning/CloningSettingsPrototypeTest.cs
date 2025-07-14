using Content.Shared.Cloning;

namespace Content.IntegrationTests.Tests.Cloning;

public sealed class CloningSettingsPrototypeTest
{
    /// <summary>
    /// Checks that the components named in every <see cref="CloningSettingsPrototype"/> are valid components known to the server.
    /// This is used instead of <see cref="ComponentNameSerializer"/> because we only care if the components are registered with the server,
    /// and instead of a <see cref="ComponentRegistry"/> because we only need component names.
    /// </summary>
    [Test]
    public async Task ValidatePrototypes()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var protoMan = server.ProtoMan;
        var compFactory = server.EntMan.ComponentFactory;

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                var protos = protoMan.EnumeratePrototypes<CloningSettingsPrototype>();
                foreach (var proto in protos)
                {
                    foreach (var compName in proto.Components)
                    {
                        Assert.That(compFactory.TryGetRegistration(compName, out _),
                            $"Failed to find a component named {compName} for {nameof(CloningSettingsPrototype)} \"{proto.ID}\""
                        );
                    }

                    foreach (var eventCompName in proto.EventComponents)
                    {
                        Assert.That(compFactory.TryGetRegistration(eventCompName, out _),
                            $"Failed to find a component named {eventCompName} for {nameof(CloningSettingsPrototype)} \"{proto.ID}\""
                        );
                    }
                }
            });
        });

        await pair.CleanReturnAsync();
    }
}
