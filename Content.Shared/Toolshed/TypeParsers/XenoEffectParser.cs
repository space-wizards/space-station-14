using Content.Shared.Xenoarchaeology.Artifact;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Shared.Toolshed.TypeParsers;

/// <summary>
/// Custom type parser for toolshed commands
/// that lets choose entity prototype of XenoArtifact effect.
/// </summary>
public sealed partial class XenoEffectParser : CustomCompletionParser<ProtoId<EntityPrototype>>
{
    [Dependency] private IEntitySystemManager _systemManager = default!;

    public override CompletionResult TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        var hint = ToolshedCommand.GetArgHint(arg, typeof(ProtoId<EntityPrototype>));

        var artifact = _systemManager.GetEntitySystem<SharedXenoArtifactSystem>();

        return CompletionResult.FromHintOptions(artifact.EffectPrototypeIds, hint);
    }
}
