using System.Linq;
using Content.Shared.Verbs;
using Robust.Shared.RTShell;
using Robust.Shared.RTShell.Syntax;
using Robust.Shared.RTShell.TypeParsers;

namespace Content.Server.NewCon.Commands.Verbs;

[ConsoleCommand]
public sealed class RunVerbAsCommand : ConsoleCommand
{
    private SharedVerbSystem? _verb;

    [CommandImplementation]
    public IEnumerable<EntityUid> RunVerbAs(
            [CommandInvocationContext] IInvocationContext ctx,
            [PipedArgument] IEnumerable<EntityUid> input,
            [CommandArgument] Block<EntityUid> runner,
            [CommandArgument] string verb
        )
    {
        _verb ??= GetSys<SharedVerbSystem>();
        verb = verb.ToLowerInvariant();
        var runnerEid = runner.Invoke(null, ctx);

        if (EntityManager.Deleted(runnerEid) && runnerEid != default)
            ctx.ReportError(new DeadEntity(runnerEid));

        if (ctx.GetErrors().Any())
            yield break;

        foreach (var i in input)
        {
            var verbs = _verb.GetLocalVerbs(i, runnerEid, Verb.VerbTypes, true);

            // if the "verb name" is actually a verb-type, try run any verb of that type.
            var verbType = Verb.VerbTypes.FirstOrDefault(x => x.Name == verb);
            if (verbType != null)
            {
                var verbTy = verbs.FirstOrDefault(v => v.GetType() == verbType);
                if (verbTy != null)
                {
                    _verb.ExecuteVerb(verbTy, runnerEid, i, forced: true);
                    yield return i;
                }
            }

            foreach (var verbTy in verbs)
            {
                if (verbTy.Text.ToLowerInvariant() == verb)
                {
                    _verb.ExecuteVerb(verbTy, runnerEid, i, forced: true);
                    yield return i;
                }
            }
        }
    }
}
