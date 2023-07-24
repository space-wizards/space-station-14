using System.Collections;
using System.Linq;
using System.Reflection;
using Robust.Shared.Utility;

namespace Content.Server.NewCon;

// This is for information about commands that can be queried, i.e. return type possibilities.

public sealed partial class NewConManager
{
    private readonly Dictionary<Type, List<(ConsoleCommand, string?)>> _commandPipeValueMap = new();
    private readonly Dictionary<(ConsoleCommand, string?), HashSet<Type>> _commandReturnValueMap = new();

    private void InitializeQueries()
    {
        foreach (var (_, cmd) in _commands)
        {
            foreach (var (subcommand, methods) in cmd.GetGenericImplementations().BySubCommand())
            {
                foreach (var method in methods)
                {
                    var piped = method.ConsoleGetPipedArgument()?.ParameterType;

                    if (piped is null)
                        piped = typeof(void);

                    var list = GetTypeImplList(piped);
                    var invList = GetCommandRetValuesInternal((cmd, subcommand));
                    list.Add((cmd, subcommand == "" ? null : subcommand));
                    if (cmd.TryGetReturnType(subcommand, piped, Array.Empty<Type>(), out var retType) || method.ReturnType.Constructable())
                    {
                        Logger.Debug($"Registering {cmd} as returning {(retType ?? method.ReturnType).PrettyName()}");
                        invList.Add((retType ?? method.ReturnType));
                    }
                }
            }
        }
    }

    public IEnumerable<(ConsoleCommand, string?)> CommandsFittingConstraint(Type input, Type output)
    {
        foreach (var (command, subcommand) in CommandsTakingType(input))
        {
            if (command.HasTypeParameters)
                continue; // We don't consider these right now.

            var impls = command.GetConcreteImplementations(input, Array.Empty<Type>(), subcommand);

            foreach (var impl in impls)
            {
                Logger.Debug($"{impl.ReturnType.PrettyName()}");
                if (impl.ReturnType.IsAssignableTo(output))
                    yield return (command, subcommand);
            }
        }
    }

    public IEnumerable<(ConsoleCommand, string?)> CommandsTakingType(Type t)
    {
        var output = new Dictionary<(string, string?), (ConsoleCommand, string?)>();
        foreach (var type in AllSteppedTypes(t))
        {
            var list = GetTypeImplList(type);
            if (type.IsGenericType)
            {
                list = list.Concat(GetTypeImplList(type.GetGenericTypeDefinition())).ToList();
            }
            foreach (var entry in list)
            {
                output.TryAdd((entry.Item1.Name, entry.Item2), entry);
            }
        }

        return output.Values;
    }

    private Dictionary<Type, HashSet<Type>> _typeCache = new();

    public IEnumerable<Type> AllSteppedTypes(Type t, bool allowVariants = true)
    {
        if (_typeCache.TryGetValue(t, out var cache))
            return cache;
        cache = new(AllSteppedTypesInner(t, allowVariants));
        _typeCache[t] = cache;

        return cache;
    }

    private IEnumerable<Type> AllSteppedTypesInner(Type t, bool allowVariants)
    {
        Type oldT;
        do
        {
            yield return t;

            if (t.IsGenericType && allowVariants)
            {
                foreach (var variant in t.GetVariants(this))
                {
                    yield return variant;
                }
            }

            foreach (var @interface in t.GetInterfaces())
            {
                foreach (var innerT in AllSteppedTypes(@interface, allowVariants))
                {
                    yield return innerT;
                }
            }

            if (t.BaseType is { } baseType)
            {
                foreach (var innerT in AllSteppedTypes(baseType, allowVariants))
                {
                    yield return innerT;
                }
            }

            oldT = t;
            t = t.StepDownConstraints();
        } while (t != oldT);
    }

    public IReadOnlySet<Type> GetCommandRetValues((ConsoleCommand, string?) command)
        => GetCommandRetValuesInternal(command);

    private HashSet<Type> GetCommandRetValuesInternal((ConsoleCommand, string?) command)
    {
        if (!_commandReturnValueMap.TryGetValue(command, out var l))
        {
            l = new();
            _commandReturnValueMap[command] = l;
        }

        return l;
    }

    private List<(ConsoleCommand, string?)> GetTypeImplList(Type t)
    {
        if (!t.Constructable())
        {
            if (t.IsGenericParameter)
            {
                var constraints = t.GetGenericParameterConstraints();

                // for now be dumb.
                if (constraints.Length > 0 && !constraints.First().IsConstructedGenericType)
                    return GetTypeImplList(constraints.First());
                return GetTypeImplList(typeof(object));
            }

            t = t.GetGenericTypeDefinition();
        }

        if (t.IsGenericType && !t.IsConstructedGenericType)
        {
            t = t.GetGenericTypeDefinition();
        }

        if (!_commandPipeValueMap.TryGetValue(t, out var l))
        {
            l = new();
            _commandPipeValueMap[t] = l;
        }

        return l;
    }
}
