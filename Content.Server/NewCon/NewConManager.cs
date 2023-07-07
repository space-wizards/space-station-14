using System.Reflection;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Reflection;

namespace Content.Server.NewCon;

public sealed class NewConManager
{
    [Dependency] private readonly IConsoleHost _conHost = default!;
    [Dependency] private readonly IDynamicTypeFactory _typeFactory = default!;
    [Dependency] private readonly IReflectionManager _reflection = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private ISawmill _log = default!;

    private readonly Dictionary<string, ConsoleCommand> _commands = new();

    public void Initialize()
    {
        _log = _logManager.GetSawmill("newcon");

        var tys = _reflection.FindTypesWithAttribute<ConsoleCommandAttribute>();
        foreach (var ty in tys)
        {
            if (!ty.IsAssignableTo(typeof(ConsoleCommand)))
            {
                _log.Error($"The type {ty.AssemblyQualifiedName} has {nameof(ConsoleCommandAttribute)} without being a child of {nameof(ConsoleCommand)}");
                continue;
            }

            var name = ty.GetCustomAttribute<ConsoleCommandAttribute>()!.Name;

            if (name is null)
            {
                var typeName = ty.Name;
                const string commandStr = "Command";

                if (!typeName.EndsWith(commandStr))
                {
                    throw new InvalidComponentNameException($"Component {ty} must end with the word Component");
                }

                name = typeName[..^commandStr.Length].ToLowerInvariant();
            }

            var command = (ConsoleCommand)_typeFactory.CreateInstance(ty, false, true);

            _commands.Add(name, command);
        }

        _conHost.RegisterCommand("|", Callback);
    }

    [AnyCommand]
    private void Callback(IConsoleShell shell, string argstr, string[] args)
    {
        if (!_commands.ContainsKey(args[0]))
        {
            shell.WriteError($"Unknown command {args[0]}");
        }

        _commands[args[0]].TryGetImplementation(null, null, out var impl);

        if (impl is not null)
        {
            var val = impl.Invoke(new CommandInvocationArguments() {Arguments = new(), PipedArgument = null});

            shell.WriteLine($"{val}");
            return;
        }

        throw new NotImplementedException("Missing implementation for c ommand");
    }
}
