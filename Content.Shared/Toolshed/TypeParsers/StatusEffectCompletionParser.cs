using System.Linq;
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

        if (!_prototype.Categories.TryGetValue(StatusEffectCategoryId, out var found))
            return CompletionResult.Empty;

        var projected = found.Select(x => x.ID);
        return CompletionResult.FromHintOptions(projected, hint);
    }
}
