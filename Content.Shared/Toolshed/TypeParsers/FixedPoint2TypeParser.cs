using Content.Shared.FixedPoint;
using Robust.Shared.Console;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;

namespace Content.Shared.Toolshed.TypeParsers;

public sealed class FixedPoint2TypeParser : TypeParser<FixedPoint2>
{
    public override bool TryParse(ParserContext ctx, out FixedPoint2 result)
    {
        if (Toolshed.TryParse(ctx, out int? value))
        {
            result =  FixedPoint2.New(value.Value);
            return true;
        }

        if (Toolshed.TryParse(ctx, out float? fValue))
        {
            result = FixedPoint2.New(fValue.Value);
            return true;
        }

        // Toolshed's number parser (NumberBaseTypeParser) should have assigned ctx.Error so we don't have to.
        DebugTools.AssertNotNull(ctx.Error);
        result = FixedPoint2.Zero;
        return false;
    }

    public override CompletionResult? TryAutocomplete(ParserContext parserContext, CommandArgument? arg)
    {
        return CompletionResult.FromHint(GetArgHint(arg));
    }
}
