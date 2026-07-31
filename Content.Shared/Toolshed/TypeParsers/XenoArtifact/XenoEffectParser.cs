using System.Linq;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Shared.Toolshed.TypeParsers.XenoArtifact;

/// <summary>
/// Custom type parser for toolshed commands
/// that lets choose entity prototype of XenoArtifact effect.
/// </summary>
public sealed partial class XenoEffectParser : CustomCompletionParser<ProtoId<EntityPrototype>>
{
    private static readonly ProtoId<EntityCategoryPrototype> EffectCategoryId = "XenoArtifactEffects";

    [Dependency] private IPrototypeManager _prototype= default!;

    public override CompletionResult TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        var hint = ToolshedCommand.GetArgHint(arg, typeof(ProtoId<EntityPrototype>));

        var categories = _prototype.Categories;
        if (!categories.TryGetValue(EffectCategoryId, out var found))
            return CompletionResult.Empty;

        var projected = found.Select(x => x.ID);
        return CompletionResult.FromHintOptions(projected, hint);

    }
}
