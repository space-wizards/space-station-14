using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Content.Server.NewCon.Errors;
using Robust.Shared.Utility;

using Invocable = System.Func<Content.Server.NewCon.CommandInvocationArguments, object?>;

namespace Content.Server.NewCon;

// TODO:
// SANDBOX SAFETY
// JESUS FUCK SANDBOX SAFETY

public abstract partial class ConsoleCommand
{
    [Dependency] protected readonly NewConManager ConManager = default!;

    public string Name { get; }

    public bool HasSubCommands { get; }

    public readonly Dictionary<string, SortedDictionary<string, Type>>? Parameters;

    public virtual Type[] TypeParameterParsers => Array.Empty<Type>();

    public bool HasTypeParameters => TypeParameterParsers.Length != 0;

    private readonly Dictionary<string, ConsoleCommandImplementor> Implementors = new();

    public IEnumerable<string> Subcommands => Implementors.Keys.Where(x => x != "");

    public ConsoleCommand()
    {
        var name = GetType().GetCustomAttribute<ConsoleCommandAttribute>()!.Name;

        if (name is null)
        {
            var typeName = GetType().Name;
            const string commandStr = "Command";

            if (!typeName.EndsWith(commandStr))
            {
                throw new InvalidComponentNameException($"Component {GetType()} must end with the word Component");
            }

            name = typeName[..^commandStr.Length].ToLowerInvariant();
        }

        Name = name;
        HasSubCommands = false;
        Implementors[""] =
            new ConsoleCommandImplementor
            {
                Owner = this,
                SubCommand = null
            };

        var impls = GetGenericImplementations();
        Parameters = new();

        foreach (var impl in impls)
        {
            var myParams = new SortedDictionary<string, Type>();
            string? subCmd = null;
            if (impl.GetCustomAttribute<CommandImplementationAttribute>() is {SubCommand: { } x})
            {
                subCmd = x;
                HasSubCommands = true;
                Implementors[x] =
                    new ConsoleCommandImplementor
                    {
                        Owner = this,
                        SubCommand = x
                    };
            }

            // TODO: Error checking, all impls must have the same required attributes when not subcommands.
            foreach (var param in impl.GetParameters())
            {
                if (param.GetCustomAttribute<CommandArgumentAttribute>() is { } arg)
                {
                    if (arg.Optional)
                        continue;

                    if (Parameters.ContainsKey(param.Name!))
                        continue;

                    myParams.Add(param.Name!, param.ParameterType);
                }
            }

            if (Parameters.TryGetValue(subCmd ?? "", out var existing))
            {
                if (!existing.SequenceEqual(existing))
                {
                    throw new NotImplementedException("All command implementations of a given subcommand must share the same parameters!");
                }
            }
            else
                Parameters.Add(subCmd ?? "", myParams);

        }
    }


    public virtual bool TryGetReturnType(string? subCommand, Type? pipedType, Type[] typeArguments, [NotNullWhen(true)] out Type? type)
    {
        var impls = GetConcreteImplementations(pipedType, typeArguments, subCommand).ToList();

        if (impls.Count == 1)
        {
            type = impls.First().ReturnType;
            return true;
        }

        type = null;
        return false;

        throw new NotImplementedException($"write your own TryGetReturnType your command is too clamplicated. Got {impls.Count} implementations for {Name} {subCommand ?? "[no subcommand]"}.");
    }

    public IEnumerable<Type> AcceptedTypes(string? subCommand)
    {
        return GetGenericImplementations()
            .Where(x => x.ConsoleGetPipedArgument() is not null)
            .Where(x => x.GetCustomAttribute<CommandImplementationAttribute>()?.SubCommand == subCommand)
            .Select(x => x.ConsoleGetPipedArgument()!.ParameterType);
    }

    private Dictionary<(CommandDiscriminator, string?), List<MethodInfo>> _concreteImplementations = new();

    public List<MethodInfo> GetConcreteImplementations(Type? pipedType, Type[] typeArguments,
        string? subCommand)
    {
        var idx = (new CommandDiscriminator(pipedType, typeArguments), subCommand);
        if (_concreteImplementations.TryGetValue(idx,
                out var impl))
        {
            return impl;
        }

        impl = GetConcreteImplementationsInternal(pipedType, typeArguments, subCommand);
        _concreteImplementations[idx] = impl;
        return impl;

    }

    private List<MethodInfo> GetConcreteImplementationsInternal(Type? pipedType, Type[] typeArguments, string? subCommand)
    {
        var impls = GetGenericImplementations()
            .Where(x =>
            {
                if (x.ConsoleGetPipedArgument() is { } param)
                {
                    return pipedType?.IsAssignableToGeneric(param.ParameterType) ?? false;
                }

                return pipedType is null;
            })
            .Where(x => x.GetCustomAttribute<CommandImplementationAttribute>()?.SubCommand == subCommand)
            .Where(x =>
            {
                if (x.IsGenericMethodDefinition)
                {
                    var expectedLen = x.GetGenericArguments().Length;
                    if (x.HasCustomAttribute<TakesPipedTypeAsGeneric>())
                        expectedLen -= 1;

                    return typeArguments.Length == expectedLen;
                }

                return typeArguments.Length == 0;
            })
            .Select(x =>
        {
            if (x.IsGenericMethodDefinition)
            {
                if (x.HasCustomAttribute<TakesPipedTypeAsGeneric>())
                {
                    var paramT = x.ConsoleGetPipedArgument()!.ParameterType;
                    var t = pipedType!.Intersect(paramT);
                    return x.MakeGenericMethod(typeArguments.Append(t).ToArray());
                }
                else
                    return x.MakeGenericMethod(typeArguments);
            }

            return x;
        }).ToList();

        return impls;
    }

    public List<MethodInfo> GetGenericImplementations()
    {
        var t = GetType();

        var methods = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance);

        return methods.Where(x => x.HasCustomAttribute<CommandImplementationAttribute>()).ToList();
    }

    public bool TryGetImplementation(Type? pipedType, string? subCommand, Type[] typeArguments, [NotNullWhen(true)] out Invocable? impl)
    {
        return Implementors[subCommand ?? ""].TryGetImplementation(pipedType, typeArguments, out impl);
    }

    public bool TryParseArguments(ForwardParser parser, Type? pipedType, string? subCommand, [NotNullWhen(true)] out Dictionary<string, object?>? args, out Type[] resolvedTypeArguments, out IConError? error)
    {
        return Implementors[subCommand ?? ""].TryParseArguments(parser, subCommand, pipedType, out args, out resolvedTypeArguments, out error);
    }
}

public sealed class CommandInvocationArguments
{
    public required object? PipedArgument;
    public required IInvocationContext Context { get; set; }
    public required CommandArgumentBundle Bundle;
    public Dictionary<string, object?> Arguments => Bundle.Arguments;
    public bool Inverted => Bundle.Inverted;
    public Type? PipedArgumentType => Bundle.PipedArgumentType;
}

public sealed class CommandArgumentBundle
{
    public required Dictionary<string, object?> Arguments;
    public required bool Inverted = false;
    public required Type? PipedArgumentType;
    public required Type[] TypeArguments;
}

public record struct CommandDiscriminator(Type? PipedType, Type[] TypeArguments) : IEquatable<CommandDiscriminator?>
{
    public bool Equals(CommandDiscriminator? other)
    {
        if (other is not {} value)
            return false;

        return value.PipedType == PipedType && value.TypeArguments.SequenceEqual(TypeArguments);
    }

    public override int GetHashCode()
    {
        // poor man's hash do not judge
        var h = PipedType?.GetHashCode() ?? (int.MaxValue / 3);
        foreach (var arg in TypeArguments)
        {
            h += h ^ arg.GetHashCode();
            int.RotateLeft(h, 3);
        }

        return h;
    }
}
