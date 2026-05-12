using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Localization;

public sealed class EntityPrototypeLocalizationTest
{
    /// <summary>
    /// An explanation of why LocIds should not be used for entity prototype names/descriptions.
    /// Appended to the error message when the test is failed.
    /// </summary>
    private const string NoLocIdExplanation = "Entity prototypes should not use LocIds for names/descriptions, as localization IDs are automated for entity prototypes. See https://docs.spacestation14.com/en/ss14-by-example/fluent-and-localization.html#localizing-prototypes for more information.";

    /// <summary>
    /// Checks that no entity prototypes have a LocId as their name or description.
    /// See <see href="https://docs.spacestation14.com/en/ss14-by-example/fluent-and-localization.html#localizing-prototypes"/> for why this is important.
    /// </summary>
    [Test]
    public async Task TestNoManualEntityLocStrings()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var protoMan = server.ProtoMan;
        var locMan = server.ResolveDependency<ILocalizationManager>();

        var protos = protoMan.EnumeratePrototypes<EntityPrototype>();

        Assert.Multiple(() =>
        {
            foreach (var proto in protos)
            {
                // Check name
                if (!string.IsNullOrEmpty(proto.SetName))
                {
                    Assert.That(locMan.HasString(proto.SetName), Is.False,
                        $"Entity prototype {proto.ID} has a LocId ({proto.SetName}) as a name. {NoLocIdExplanation}");
                }

                // Check description
                if (!string.IsNullOrEmpty(proto.SetDesc))
                {
                    Assert.That(locMan.HasString(proto.SetDesc), Is.False,
                        $"Entity prototype {proto.ID} has a LocId ({proto.SetDesc}) as a description. {NoLocIdExplanation}");
                }
            }
        });

        await pair.CleanReturnAsync();
    }
}
