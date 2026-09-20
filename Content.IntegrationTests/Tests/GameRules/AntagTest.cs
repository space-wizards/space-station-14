using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Shared.Antag;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.IntegrationTests.Tests.GameRules;

/// <summary>
/// An abstract test fixture which is setup specifically for tests involving antag definitions, to ensure they work correctly!
/// </summary>
public abstract partial class AntagTest : GameTest
{
    public override PoolSettings PoolSettings => new()
    {
        Dirty = true,
        DummyTicker = false,
        Connected = true,
        InLobby = true
    };

    [SidedDependency(Side.Server)] protected AntagSelectionSystem AntagSys = default!;
    [SidedDependency(Side.Server)] protected GameTicker STicker = default!;
    [SidedDependency(Side.Server)] protected MindSystem SMind = default!;

    /// <summary>
    /// Asserts that a given player session has been properly assigned a specific antag specifier.
    /// Checks that the antag has the correct components on its entity, on its mind, and has the correct mind roles
    /// As well as ensuring that the entity has not spawned in nullspace.
    /// </summary>
    /// <param name="antag">Antag definition this session should have been assigned</param>
    /// <param name="session">Dummy session we are checking antagonist status on</param>
    protected void SAssertAntagInitialized(AntagSpecifierPrototype antag, ICommonSession session)
    {
        Assert.That(SMind.TryGetMind(session, out var mindEnt, out var mindComp),
            $"Session {session} spawned into the game as an antag but had no mind!");
        Assert.That(SEntMan.EntityExists(mindComp!.CurrentEntity),
            $"Session {session} spawned into the game as an antag, but had no entity!");
        var ent = mindComp.CurrentEntity!.Value;

        // We don't necessarily know if an antag should spawn on the station, but we know they shouldn't spawn in nullspace.
        var xform = SEntMan.GetComponent<TransformComponent>(ent);
        Assert.That(xform.MapUid, Is.Not.Null);
        Assert.That(xform.MapID, Is.Not.EqualTo(MapId.Nullspace));

        // Make sure all components were added
        foreach (var comp in antag.Components)
        {
            Assert.That(SEntMan.HasComponent(ent, comp.Value.Component.GetType()),
                $"Entity {SEntMan.ToPrettyString(ent)} owned by {session} failed to acquire {comp.Key} component, while becoming {antag.ID}");
        }

        // Make sure all mind components were added
        foreach (var comp in antag.MindComponents)
        {
            Assert.That(SEntMan.HasComponent(mindEnt, comp.Value.Component.GetType()),
                $"Mind {SEntMan.ToPrettyString(mindEnt)} owned by {session} failed to acquire {comp.Key} component, while becoming {antag.ID}");
        }

        if (antag.MindRoles != null)
        {
            Assert.Multiple(() =>
            {
                foreach (var role in antag.MindRoles)
                {
                    Assert.That(mindComp!.MindRoleContainer.ContainedEntities,
                        Has.Exactly(1).Matches<EntityUid>(x => SComp<MetaDataComponent>(x).EntityPrototype?.ID == role),
                        $"{SToPrettyString(mindEnt)} owned by {session}, failed to acquire role {role} for antagonist {antag}");
                }
            });
        }
    }
}
