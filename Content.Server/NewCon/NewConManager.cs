using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using Content.Server.NewCon.Commands.TypeParsers;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Reflection;

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

        InitializeParser();

        _conHost.RegisterCommand("|", Callback);
    }

    [AnyCommand]
    private void Callback(IConsoleShell shell, string argstr, string[] args)
    {
        if (!_commands.ContainsKey(args[0]))
        {
            shell.WriteError($"Unknown command {args[0]}");
        }

        var parser = new ForwardParser(argstr);
        parser.GetWord(); // shell is annoying, discard the first word.

        object? value = null;
        Type? lastRetType = null;
        while (parser.GetWord() is { } cmd)
        {
            var cmdImpl = _commands[cmd];
            var piped = lastRetType;
            cmdImpl.TryGetImplementation(lastRetType, null, out var impl);
            cmdImpl.TryGetReturnType(lastRetType, out lastRetType);

            if (impl is not null)
            {
                value = impl.Invoke(new CommandInvocationArguments() {Arguments = cmdImpl.ParseArguments(parser), PipedArgument = value, PipedArgumentType = piped, Inverted = false});
            }
            else
            {
                throw new NotImplementedException("Missing implementation for command");
            }
        }

        shell.WriteLine(PrettyPrintType(value));
    }

    public string PrettyPrintType(object? value)
    {
        if (value is null)
            return "null";

        if (value is EntityUid uid)
        {
            return _entity.ToPrettyString(uid);
        }

        if (value.GetType().IsAssignableTo(typeof(IEnumerable<EntityUid>)))
        {
            return string.Join(", ", ((IEnumerable<EntityUid>)value).Select(_entity.ToPrettyString));
        }

        if (value.GetType().IsAssignableTo(typeof(IEnumerable)))
        {
            return string.Join(", ", ((IEnumerable) value).Cast<object?>().Select(PrettyPrintType));
        }

        if (value.GetType().IsAssignableTo(typeof(IDictionary)))
        {
            var dict = ((IDictionary) value).GetEnumerator();

            var kvList = new List<string>();

            do
            {
                kvList.Add($"({PrettyPrintType(dict.Key)}, {PrettyPrintType(dict.Value)}");
            } while (dict.MoveNext());

            return $"Dictionary {{{string.Join(", ", kvList)}}}";
        }

        return value.ToString() ?? "[unrepresentable]";
    }
}
