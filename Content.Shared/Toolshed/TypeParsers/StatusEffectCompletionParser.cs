using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Shared.Toolshed.TypeParsers;

public sealed partial class StatusEffectCompletionParser : CustomCompletionParser<EntProtoId>
{
    private static readonly ProtoId<EntityCategoryPrototype> StatusEffectCategoryId = "StatusEffects";

    [Dependency] private IPrototypeManager _prototype = default!;

    public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        var hint = ToolshedCommand.GetArgHint(arg, typeof(ProtoId<EntityPrototype>));
        var completionOptions = CompletionHelper.EntityPrototypes(ctx.GetWord(), StatusEffectCategoryId, _prototype);
        return CompletionResult.FromHintOptions(completionOptions, hint);
    }
}
