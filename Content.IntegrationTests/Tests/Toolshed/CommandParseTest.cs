using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.IntegrationTests.Tests.Toolshed;

[TestFixture]
public sealed class CommandRunTest : ToolshedTest
{
    [Test]
    public async Task SimpleCommandRun()
    {
        await Server.WaitAssertion(() =>
        {
            ParseCommand("entities");
            ParseCommand("entities select 1");
            ParseCommand("entities with Item select 1");

            ExpectError<OutOfInputError>();
            ParseCommand("entities with");

            ExpectError<NoImplementationError>();
            ParseCommand("player:list with MetaData");

            ExpectError<ExpressionOfWrongType>();
            ParseCommand("player:list", expectedType: typeof(IEnumerable<EntityUid>));

            ParseCommand("entities not with MetaData");
            ParseCommand("with MetaData select 2 any", inputType: typeof(List<EntityUid>));

            ParseCommand("entities not with MetaData => $myEntities");
            ParseCommand("=> $fooBar with MetaData", inputType: typeof(List<EntityUid>));
        });
    }

    [Test]
    public async Task EntityUidTypeParser()
    {
        await Server.WaitAssertion(() =>
        {
            ParseCommand("ent 1");
            ParseCommand("ent c1");

            ExpectError<InvalidEntityUid>();
            ParseCommand("ent foodigity");
        });
    }

    [Test]
    public async Task QuantityTypeParser()
    {
        await Server.WaitAssertion(() =>
        {
            ParseCommand("entities select 100");
            ParseCommand("entities select 50%");

            ExpectError<InvalidQuantity>();
            ParseCommand("entities select -1");

            ExpectError<InvalidQuantity>();
            ParseCommand("entities select 200%");

            ExpectError<InvalidQuantity>();
            ParseCommand("entities select hotdog");
        });
    }

    [Test]
    public async Task ComponentTypeParser()
    {
        await Server.WaitAssertion(() =>
        {
            ParseCommand("entities with MetaData");

            ExpectError<UnknownComponentError>();
            ParseCommand("entities with Foodiddy");

            ExpectError<UnknownComponentError>();
            ParseCommand("entities with MetaDataComponent");
        });
    }
}
