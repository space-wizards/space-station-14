using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Toolshed;

namespace Content.IntegrationTests.Tests.Toolshed;

[TestFixture]
public sealed class LocTest : ToolshedTest
{
    [Test]
    public async Task AllCommandsHaveDescriptions()
    {
        await Server.WaitAssertion(() =>
        {
            Assert.That(InvokeCommand("cmd:list where { cmd:descloc loc:tryloc isnull }", out var res));
            Assert.That((IEnumerable<CommandSpec>)res, Is.Empty, "All commands must have localized descriptions.");
        });
    }
}
