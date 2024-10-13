using Content.Shared.Roles;

namespace Content.IntegrationTests.Tests.Minds;

[TestFixture]
public sealed class RoleTests
{
    [Test]
    public async Task ValidatePrototypes()
    {
        await using var pair = await PoolManager.GetServerClient();

        Assert.Multiple(() =>
        {
            foreach (var (proto, comp) in pair.GetPrototypesWithComponent<MindRoleComponent>())
            {
                // According to MindGetAllRoleInfo(), having both prototypes on a single role is unsupported.
                Assert.That(comp.AntagPrototype == null || comp.JobPrototype == null, $"Role {proto.ID} has both a job and antag prototype.");
                Assert.That(!comp.ExclusiveAntag || comp.Antag, $"Role {proto.ID} is marked as an exclusive antag, despite not being an antag.");
                Assert.That(comp.Antag || comp.AntagPrototype == null, $"Role {proto.ID} has an antag prototype, despite not being an antag.");
                Assert.That(!comp.Antag || comp.AntagPrototype != null , $"Role {proto.ID} is an antag, despite not having a antag prototype.");
            }
        });

        await pair.CleanReturnAsync();
    }
}
