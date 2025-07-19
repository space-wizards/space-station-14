using System.Linq;
using Content.Server.Roles;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.GameObjects;
using Robust.Shared.Reflection;

namespace Content.IntegrationTests.Tests.Minds;

[TestFixture]
public sealed class RoleTests
{
    /// <summary>
    /// Check that any prototype with a <see cref="MindRoleComponent"/> is properly configured
    /// </summary>
    [Test]
    public async Task ValidateRolePrototypes()
    {
        await using var pair = await PoolManager.GetServerClient();

        var jobComp = pair.Server.ResolveDependency<IComponentFactory>().GetComponentName<JobRoleComponent>();

        Assert.Multiple(() =>
        {
            foreach (var (proto, comp) in pair.GetPrototypesWithComponent<MindRoleComponent>())
            {
                Assert.That(comp.AntagPrototype == null || comp.JobPrototype == null, $"Role {proto.ID} has both a job and antag prototype.");
                Assert.That(!comp.ExclusiveAntag || comp.Antag, $"Role {proto.ID} is marked as an exclusive antag, despite not being an antag.");
                Assert.That(comp.Antag || comp.AntagPrototype == null, $"Role {proto.ID} has an antag prototype, despite not being an antag.");

                if (comp.JobPrototype != null)
                    Assert.That(proto.Components.ContainsKey(jobComp), $"Role {proto.ID} is a job, despite not having a job prototype.");

                // It is possible that this is meant to be supported? Though I would assume that it would be for
                // admin / prototype uploads, and that pre-defined roles should still check this.
                Assert.That(!comp.Antag || comp.AntagPrototype != null , $"Role {proto.ID} is an antag, despite not having a antag prototype.");
            }
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Check that any prototype with a <see cref="JobRoleComponent"/> also has a properly configured
    /// <see cref="MindRoleComponent"/>
    /// </summary>
    [Test]
    public async Task ValidateJobPrototypes()
    {
        await using var pair = await PoolManager.GetServerClient();

        var mindCompId = pair.Server.ResolveDependency<IComponentFactory>().GetComponentName<MindRoleComponent>();

        Assert.Multiple(() =>
        {
            foreach (var (proto, comp) in pair.GetPrototypesWithComponent<JobRoleComponent>())
            {
                if (proto.Components.TryGetComponent(mindCompId, out var mindComp))
                    Assert.That(((MindRoleComponent)mindComp).JobPrototype, Is.Not.Null);
            }
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Check that any prototype with a component that inherits from <see cref="BaseMindRoleComponent"/> also has a
    /// <see cref="MindRoleComponent"/>
    /// </summary>
    [Test]
    public async Task ValidateRolesHaveMindRoleComp()
    {
        await using var pair = await PoolManager.GetServerClient();

        var refMan = pair.Server.ResolveDependency<IReflectionManager>();
        var mindCompId = pair.Server.ResolveDependency<IComponentFactory>().GetComponentName<MindRoleComponent>();

        var compTypes = refMan.GetAllChildren(typeof(BaseMindRoleComponent))
            .Append(typeof(RoleBriefingComponent))
            .Where(x => !x.IsAbstract);

        Assert.Multiple(() =>
        {
            foreach (var comp in compTypes)
            {
                foreach (var proto in pair.GetPrototypesWithComponent(comp))
                {
                    Assert.That(proto.Components.ContainsKey(mindCompId), $"Role {proto.ID} does not have a {nameof(MindRoleComponent)} despite having a {comp.Name}");
                }
            }
        });

        await pair.CleanReturnAsync();
    }
}
