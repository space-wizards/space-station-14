using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Shared.Toolshed.TypeParsers.XenoArtifact;

/// <summary>
/// Custom type parser for toolshed commands
/// that lets choose entity prototype of XenoArtifact trigger.
/// </summary>
public sealed partial class XenoArtifactTriggerParser : CustomCompletionParser<ProtoId<EntityPrototype>>
{
    private static readonly ProtoId<EntityCategoryPrototype> TriggerCategoryId = "XenoArtifactTriggers";

    [Dependency] private IPrototypeManager _prototype = default!;

    public override CompletionResult TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        var hint = ToolshedCommand.GetArgHint(arg, typeof(ProtoId<EntityPrototype>));
        var completionOptions = CompletionHelper.EntityPrototypes(ctx.GetWord(), TriggerCategoryId, _prototype);
        return CompletionResult.FromHintOptions(completionOptions, hint);

    }
}
