using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
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
        var locMan = Server.ResolveDependency<ILocalizationManager>();
        var toolMan = Server.ResolveDependency<ToolshedManager>();
        var locStrings = new HashSet<string>();

        var ignored = new HashSet<Assembly>()
            {typeof(LocTest).Assembly};

        await Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var cmd in toolMan.DefaultEnvironment.AllCommands())
                {
                    if (ignored.Contains(cmd.Cmd.GetType().Assembly))
                        continue;

                    var descLoc = cmd.DescLocStr();
                    Assert.That(locStrings.Add(descLoc), $"Duplicate command description key: {descLoc}");
                    Assert.That(locMan.TryGetString(descLoc, out _), $"Failed to get command description for command {cmd.FullName()}");
                }
            });
        });
    }
}
