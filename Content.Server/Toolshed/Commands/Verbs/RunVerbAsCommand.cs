using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Verbs;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Server.Toolshed.Commands.Verbs;

[ToolshedCommand, AdminCommand(AdminFlags.Moderator)]
public sealed class RunVerbAsCommand : ToolshedCommand
{
    private SharedVerbSystem? _verb;

    [CommandImplementation]
    public IEnumerable<NetEntity> RunVerbAs(
            IInvocationContext ctx,
            [PipedArgument] IEnumerable<NetEntity> input,
            EntityUid runner,
            string verb
        )
    {
        _verb ??= GetSys<SharedVerbSystem>();
        verb = verb.ToLowerInvariant();

        foreach (var i in input)
        {
            if (EntityManager.Deleted(runner) && runner.IsValid())
                ctx.ReportError(new DeadEntity(runner));

            if (ctx.GetErrors().Any())
                yield break;

            var eId = EntityManager.GetEntity(i);
            var verbs = _verb.GetLocalVerbs(eId, runner, Verb.VerbTypes, true);

            // if the "verb name" is actually a verb-type, try run any verb of that type.
            var verbType = Verb.VerbTypes.FirstOrDefault(x => x.Name == verb);
            if (verbType != null)
            {
                var verbTy = verbs.FirstOrDefault(v => v.GetType() == verbType);
                if (verbTy != null)
                {
                    _verb.ExecuteVerb(verbTy, runner, eId, forced: true);
                    yield return i;
                }
            }

            foreach (var verbTy in verbs)
            {
                if (verbTy.Text.ToLowerInvariant() == verb)
                {
                    _verb.ExecuteVerb(verbTy, runner, eId, forced: true);
                    yield return i;
                }
            }
        }
    }
}
