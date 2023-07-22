using System.Linq;
using Content.Server.NewCon.Syntax;
using Content.Shared.Verbs;

namespace Content.Server.NewCon.Commands.Verbs;

[ConsoleCommand]
public sealed class RunVerbAsCommand : ConsoleCommand
{
    [CommandImplementation]
    public IEnumerable<EntityUid> RunVerbAs(
            [CommandInvocationContext] IInvocationContext ctx,
            [PipedArgument] IEnumerable<EntityUid> input,
            [CommandArgument] Expression<EntityUid> runner,
            [CommandArgument] string verb
        )
    {
        verb = verb.ToLowerInvariant();
        var runnerEid = runner.Invoke(null, ctx);
        var sys = EntitySystem.Get<SharedVerbSystem>();
        foreach (var i in input)
        {
            var verbs = sys.GetLocalVerbs(i, runnerEid, Verb.VerbTypes, true);

            // if the "verb name" is actually a verb-type, try run any verb of that type.
            var verbType = Verb.VerbTypes.FirstOrDefault(x => x.Name == verb);
            if (verbType != null)
            {
                var verbTy = verbs.FirstOrDefault(v => v.GetType() == verbType);
                if (verbTy != null)
                {
                    sys.ExecuteVerb(verbTy, runnerEid, i, forced: true);
                    yield return i;
                }
            }

            foreach (var verbTy in verbs)
            {
                if (verbTy.Text.ToLowerInvariant() == verb)
                {
                    sys.ExecuteVerb(verbTy, runnerEid, i, forced: true);
                    yield return i;
                }
            }
        }
    }
}
