using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server.NewCon.Syntax;

public sealed class Expression
{
    public List<ParsedCommand> Commands;

    public static bool TryParse(ForwardParser parser, Type? pipedType, Type? targetOutput, bool once, [NotNullWhen(true)] out Expression? expr)
    {
        var cmds = new List<ParsedCommand>();
        while (ParsedCommand.TryParse(parser, pipedType, out var cmd))
        {
            if (cmds.Count != 1 && once)
            {
                expr = null;
                return false;
            }

            pipedType = cmd.ReturnType;
            cmds.Add(cmd);
            if (cmd.ReturnType == targetOutput)
                goto done;
        }

        if (cmds.Last().ReturnType != targetOutput && targetOutput is not null)
        {
            Logger.Debug("Bailing due to wrong return type");
            expr = null;
            return false;
        }

        done:
        expr = new Expression(cmds);
        return true;
    }

    public object? Invoke(object? input)
    {
        var ret = input;
        foreach (var cmd in Commands)
        {
            ret = cmd.Invoke(ret);
        }

        return ret;
    }


    private Expression(List<ParsedCommand> commands)
    {
        Commands = commands;
    }
}
