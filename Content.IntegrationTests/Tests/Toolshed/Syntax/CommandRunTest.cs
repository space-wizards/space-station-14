using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;

namespace Content.IntegrationTests.Tests.Toolshed.Syntax;

[TestFixture]
public sealed class CommandRunTest : ToolshedTest
{
    [Test]
    public async Task SimpleCommandRun()
    {
        await ParseCommand("entities");
        await ParseCommand("entities select 1");
        await ParseCommand("entities with Item select 1");

        // the fuck
        ExpectError<OutOfInputError>();
        await ParseCommand("entities with");

        ExpectError<NoImplementationError>();
        await ParseCommand("player:list with Item");
    }
}
