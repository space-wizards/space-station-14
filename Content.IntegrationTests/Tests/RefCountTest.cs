using Content.Shared.Actions;
using Content.Shared.StatusEffect;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests;

[TestFixture]
public sealed class RefCountTest
{
    // some entity that won't have ActionsComponent
    public EntProtoId Crowbar = "Crowbar";

    [Test]
    public async Task RefCountWorks()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var sys = entMan.System<RefCountSystem>();
        await server.WaitAssertion(() =>
        {
            var query = entMan.GetEntityQuery<ActionsComponent>();

            // won't have the component when spawned
            var uid = entMan.Spawn(Crowbar);
            Assert.That(query.HasComp(uid), Is.False);
            Assert.That(sys.GetCount(uid, "Actions"), Is.EqualTo(0));

            // must have it after adding
            Assert.That(sys.Add<ActionsComponent>(uid), Is.True);
            Assert.That(query.HasComp(uid), Is.True);
            Assert.That(sys.GetCount(uid, "Actions"), Is.EqualTo(1));

            // component isn't added now, only count is incremented
            Assert.That(sys.Add<ActionsComponent>(uid), Is.False);
            Assert.That(sys.GetCount(uid, "Actions"), Is.EqualTo(2));
            Assert.That(query.HasComp(uid), Is.True);

            // remove 1 reference, component won't be removed
            Assert.That(sys.Remove<ActionsComponent>(uid), Is.False);
            Assert.That(sys.GetCount(uid, "Actions"), Is.EqualTo(1));
            Assert.That(query.HasComp(uid), Is.True);

            // component must be removed now
            Assert.That(sys.Remove<ActionsComponent>(uid), Is.True);
            Assert.That(sys.GetCount(uid, "Actions"), Is.EqualTo(0));
            Assert.That(query.HasComp(uid), Is.False);

            entMan.DeleteEntity(uid);
        });

        await pair.CleanReturnAsync();
    }
}
