using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Content.Server.NewCon.Errors;
using Content.Server.NewCon.TypeParsers;
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

    public readonly SortedDictionary<string, Type>? Parameters;

    public virtual Type[] TypeParameterParsers => Array.Empty<Type>();

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

        foreach (var impl in impls)
        {
            if (impl.GetCustomAttribute<CommandImplementationAttribute>() is {SubCommand: { } x})
            {
                HasSubCommands = true;
                Implementors[x] =
                    new ConsoleCommandImplementor
                    {
                        Owner = this,
                        SubCommand = x
                    };
            }

            Parameters = new();

            // TODO: Error checking, all impls must have the same required attributes when not subcommands.
            foreach (var param in impl.GetParameters())
            {
                if (param.GetCustomAttribute<CommandArgumentAttribute>() is { } arg)
                {
                    if (arg.Optional)
                        continue;

                    if (Parameters.ContainsKey(param.Name!))
                        continue;

                    Parameters.Add(param.Name!, param.ParameterType);
                }
            }
        }
    }


    public virtual bool TryGetReturnType(string? subCommand, Type? pipedType, Type[] typeArguments, out Type? type)
    {
        var impls = GetConcreteImplementations(pipedType, typeArguments, subCommand).ToList();

        if (impls.Count == 1)
        {
            type = impls.First().ReturnType;
            return true;
        }

        throw new NotImplementedException($"write your own TryGetReturnType your command is too clamplicated. Got {impls.Count} implementations for {subCommand ?? "[no subcommand]"}.");
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
            .Where(x => x.GetCustomAttribute<CommandImplementationAttribute>()?.SubCommand == subCommand)
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

    public bool TryParseArguments(ForwardParser parser, [NotNullWhen(true)] out Dictionary<string, object?>? args, out Type[] resolvedTypeArguments, out IConError? error)
    {
        if (Parameters is null)
            throw new NotImplementedException("dhfshbfghd");

        var output = new Dictionary<string, object?>();
        resolvedTypeArguments = new Type[TypeParameterParsers.Length];

        for (var i = 0; i < TypeParameterParsers.Length; i++)
        {
            if (!ConManager.TryParse(parser, TypeParameterParsers[i], out var parsed, out error) || parsed is not IAsType ty)
            {
                resolvedTypeArguments = Array.Empty<Type>();
                args = null;
                return false;
            }

            resolvedTypeArguments[i] = ty.AsType();
        }

        foreach (var param in Parameters)
        {
            if (!ConManager.TryParse(parser, param.Value, out var parsed, out error))
            {
                args = null;
                return false;
            }
            output[param.Key] = parsed;
        }

        args = output;
        error = null;
        return true;
    }
}

public sealed class ConsoleCommandImplementor
{
    public required ConsoleCommand Owner;

    public required string? SubCommand;

    public Dictionary<CommandDiscriminator, Invocable> Implementations = new();

    public bool TryGetImplementation(Type? pipedType, Type[] typeArguments, [NotNullWhen(true)] out Invocable? impl)
    {
        var discrim = new CommandDiscriminator(pipedType, typeArguments);

        if (!Owner.TryGetReturnType(SubCommand, pipedType, typeArguments, out var ty))
        {
            impl = null;
            return false;
        }

        if (Implementations.TryGetValue(discrim, out impl))
            return true;

        // Okay we need to build a new shim.

        var possibleImpls = Owner.GetConcreteImplementations(pipedType, typeArguments, SubCommand);

        IEnumerable<MethodInfo> impls;

        if (pipedType is null)
        {
            impls = possibleImpls.Where(x =>
                        x.ConsoleGetPipedArgument() is {} param && param.ParameterType.CanBeEmpty()
                        || x.ConsoleGetPipedArgument() is null
                        || x.GetParameters().Length == 0);
        }
        else
        {
            impls = possibleImpls.Where(x =>
                x.ConsoleGetPipedArgument() is {} param && pipedType!.IsAssignableTo(param.ParameterType)
                || x.IsGenericMethodDefinition);
        }

        var implArray = impls.ToArray();
        if (implArray.Length == 0)
        {
            if (typeArguments.Length == 0)
                Logger.Error($"Found zero potential implementations for {pipedType} > {Owner.GetType()}");
            else
                Logger.Error($"Found zero potential implementations for {pipedType?.Name ?? "void"} > {Owner.GetType().Name}<{string.Join(", ", typeArguments.Select(x => x.Name))}>");
            return false;
        }

        var unshimmed = implArray.First();

        var args = Expression.Parameter(typeof(CommandInvocationArguments));

        var paramList = new List<Expression>();

        foreach (var param in unshimmed.GetParameters())
        {
            if (param.GetCustomAttribute<PipedArgumentAttribute>() is { } _)
            {
                if (pipedType is null)
                {
                    paramList.Add(param.ParameterType.CreateEmptyExpr());
                }
                else
                {
                    // (ParameterType)(args.PipedArgument)
                    paramList.Add(Expression.Convert(Expression.Field(args, nameof(CommandInvocationArguments.PipedArgument)), pipedType));
                }

                continue;
            }

            if (param.GetCustomAttribute<CommandArgumentAttribute>() is { } arg)
            {
                // (ParameterType)(args.Arguments[param.Name])
                paramList.Add(Expression.Convert(
                    Expression.MakeIndex(
                        Expression.Property(args, nameof(CommandInvocationArguments.Arguments)),
                        typeof(Dictionary<string, object?>).FindIndexerProperty(),
                        new [] {Expression.Constant(param.Name)}),
                param.ParameterType));
                continue;
            }

            if (param.GetCustomAttribute<CommandInvertedAttribute>() is { } _)
            {
                // args.Inverted
                paramList.Add(Expression.Property(args, nameof(CommandInvocationArguments.Inverted)));
                continue;
            }

            if (param.GetCustomAttribute<CommandInvocationContextAttribute>() is { } _)
            {
                // args.Context
                paramList.Add(Expression.Property(args, nameof(CommandInvocationArguments.Context)));
                continue;
            }

        }

        Expression partialShim = Expression.Call(Expression.Constant(Owner), unshimmed, paramList);

        if (unshimmed.ReturnType == typeof(void))
            partialShim = Expression.Block(partialShim, Expression.Constant(null));
        else if (ty is not null && ty.IsValueType)
            partialShim = Expression.Convert(partialShim, typeof(object)); // Have to box primitives.

        var lambda = Expression.Lambda<Invocable>(partialShim, args);

        Implementations[discrim] = lambda.Compile();
        impl = Implementations[discrim];
        return true;
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
