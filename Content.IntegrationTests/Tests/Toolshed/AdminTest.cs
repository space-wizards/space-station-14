using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Toolshed;

namespace Content.IntegrationTests.Tests.Toolshed;

[TestFixture]
public sealed class AdminTest : ToolshedTest
{
    [Test]
    public async Task AllCommandsHavePermissions()
    {
        await Server.WaitAssertion(() =>
        {
            Assert.That(InvokeCommand("cmd:list where { acmd:perms isnull }", out var res));
            Assert.That((IEnumerable<CommandSpec>) res, Is.Empty, "All commands must have admin permissions set up.");
        });
    }
}
