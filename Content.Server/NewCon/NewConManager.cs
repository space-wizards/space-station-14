using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using Content.Server.NewCon.Invocation;
using Content.Server.NewCon.Syntax;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Reflection;
using Robust.Shared.Timing;

namespace Content.Server.NewCon;

public sealed partial class NewConManager
{
    [Dependency] private readonly IConsoleHost _conHost = default!;
    [Dependency] private readonly IDynamicTypeFactory _typeFactory = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IReflectionManager _reflection = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private ISawmill _log = default!;

    private readonly Dictionary<string, ConsoleCommand> _commands = new();

    public void Initialize()
    {
        _log = _logManager.GetSawmill("newcon");
        var watch = new Stopwatch();
        watch.Start();

        var tys = _reflection.FindTypesWithAttribute<ConsoleCommandAttribute>();
        foreach (var ty in tys)
        {
            if (!ty.IsAssignableTo(typeof(ConsoleCommand)))
            {
                _log.Error($"The type {ty.AssemblyQualifiedName} has {nameof(ConsoleCommandAttribute)} without being a child of {nameof(ConsoleCommand)}");
                continue;
            }

            var command = (ConsoleCommand)_typeFactory.CreateInstance(ty, false, true);

            _commands.Add(command.Name, command);
        }

        InitializeParser();
        InitializeQueries();

        _conHost.RegisterCommand("|", Callback);
        _log.Info($"Initialized console in {watch.Elapsed}");
    }

    public IEnumerable<(ConsoleCommand, string?)> AllCommands()
    {
        foreach (var (_, cmd) in _commands)
        {
            if (cmd.HasSubCommands)
            {
                foreach (var subcommand in cmd.Subcommands)
                {
                    yield return (cmd, subcommand);
                }
            }
            else
            {
                yield return (cmd, null);
            }
        }
    }

    public ConsoleCommand GetCommand(string commandName) => _commands[commandName];

    public bool TryGetCommand(string commandName, [NotNullWhen(true)] out ConsoleCommand? command)
    {
        return _commands.TryGetValue(commandName, out command);
    }

    [AnyCommand]
    private void Callback(IConsoleShell shell, string argstr, string[] args)
    {
        var parser = new ForwardParser(argstr[2..]);
        var ctx = new OldShellInvocationContext(shell);
        if (!Expression.TryParse(parser, null, null, false, out var expr, out var err) || parser.Index < parser.MaxIndex)
        {
            if (err is not null)
            {
                ctx.ReportError(err);
                ctx.WriteLine(err.Describe());
            }
            else
            {
                ctx.WriteLine("Got some unknown error while parsing.");
            }

            return;
        }

        var value = expr.Invoke(null, ctx);

        shell.WriteLine(PrettyPrintType(value));
    }
}
