using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Utility;
using Content.Shared.Cloning;

namespace Content.IntegrationTests.Tests.Cloning;

public sealed class CloningSettingsPrototypeTest : GameTest
{
    private static readonly string[] CloningSettingsPrototypes = GameDataScrounger.PrototypesOfKind<CloningSettingsPrototype>();

    /// <summary>
    /// Checks that the components named in every <see cref="CloningSettingsPrototype"/> are valid components known to the server.
    /// This is used instead of <see cref="ComponentNameSerializer"/> because we only care if the components are registered with the server,
    /// and instead of a <see cref="ComponentRegistry"/> because we only need component names.
    /// </summary>
    [TestCaseSource(nameof(CloningSettingsPrototypes))]
    [Description($"Checks that all components named in a {nameof(CloningSettingsPrototype)} are registered on the server.")]
    [RunOnSide(Side.Server)]
    public async Task ValidateCloningSettingsPrototype(string protoId)
    {
        var proto = SProtoMan.Index<CloningSettingsPrototype>(protoId);

        foreach (var compName in proto.Components)
        {
            Assert.That(SEntMan.ComponentFactory.TryGetRegistration(compName, out _),
                $"Failed to find a component named {compName} for {nameof(CloningSettingsPrototype)} \"{proto.ID}\""
            );
        }

        foreach (var eventCompName in proto.EventComponents)
        {
            Assert.That(SEntMan.ComponentFactory.TryGetRegistration(eventCompName, out _),
                $"Failed to find a component named {eventCompName} for {nameof(CloningSettingsPrototype)} \"{proto.ID}\""
            );
        }
    }
}
