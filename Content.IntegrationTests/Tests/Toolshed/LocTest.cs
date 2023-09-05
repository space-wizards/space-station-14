using System.Collections.Generic;
using System.Globalization;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Toolshed;

namespace Content.IntegrationTests.Tests.Toolshed;

// this is an EXACT DUPLICATE of LocTest from robust. If you modify this, modify that too.
// Anyone who fails to heed these instructions consents to being scrungled to death.
[TestFixture]
public sealed class LocTest : ToolshedTest
{
    [Test]
    public async Task AllCommandsHaveDescriptions()
    {
        await Server.WaitAssertion(() =>
        {
            Assert.That(InvokeCommand("cmd:list where { cmd:descloc loc:tryloc isnull }", out var res));
            Assert.That((IEnumerable<CommandSpec>)res!, Is.Empty, "All commands must have localized descriptions.");
        });
    }
}
