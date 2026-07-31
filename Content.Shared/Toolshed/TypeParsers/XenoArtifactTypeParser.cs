using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Shared.Toolshed.TypeParsers;

/// <summary>
/// Custom type parser for toolshed commands that will enable choosing between hand-held and
/// stationary artifact types.
/// </summary>
public sealed partial class XenoArtifactTypeParser : CustomCompletionParser<ProtoId<EntityPrototype>>
{
    private static readonly EntProtoId ArtifactDummyItem = "DummyArtifactItem";
    private static readonly EntProtoId ArtifactDummyStructure = "DummyArtifactStructure";


    public override CompletionResult TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        return CompletionResult.FromHintOptions(
            [
                new CompletionOption(ArtifactDummyItem, Loc.GetString("command-spawnartifactwithnode-spawn-artifact-item-hint")),
                new CompletionOption(ArtifactDummyStructure, Loc.GetString("command-spawnartifactwithnode-spawn-artifact-structure-hint")),
            ],
            Loc.GetString("command-spawnartifactwithnode-spawn-artifact-type-hint")
        );
    }
}
