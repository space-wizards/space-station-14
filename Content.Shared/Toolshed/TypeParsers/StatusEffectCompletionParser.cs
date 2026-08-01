using Content.Shared.StatusEffectNew;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Shared.Toolshed.TypeParsers;

public sealed partial class StatusEffectCompletionParser : CustomCompletionParser<EntProtoId>
{
    [Dependency] private IEntityManager _entityManager = default!;

    public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        var statusEffects = _entityManager.System<StatusEffectsSystem>();
        return CompletionResult.FromHintOptions(statusEffects.StatusEffectPrototypes, GetArgHint(arg));
    }
}
