using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Content.Server.NewCon.TypeParsers;
using Robust.Shared.Exceptions;
using Robust.Shared.Utility;

using Invocable = System.Func<Content.Server.NewCon.CommandInvocationArguments, object?>;

namespace Content.Server.NewCon;

// TODO:
// SANDBOX SAFETY
// JESUS FUCK SANDBOX SAFETY

public abstract class ConsoleCommand
{
    [Dependency] private readonly NewConManager _newCon = default!;

    public bool HasSubCommands { get; init; }

    public readonly SortedDictionary<string, Type>? Parameters;

    public virtual Type[] TypeParameterParsers => Array.Empty<Type>();

    private ConsoleCommandImplementor Implementor;

    public ConsoleCommand()
    {
        Implementor =
            new ConsoleCommandImplementor
            {
                Owner = this,
                SubCommand = null
            };

        var impls = GetGenericImplementations();

        foreach (var impl in impls)
        {
            if (impl.GetCustomAttribute<CommandImplementationAttribute>() is {SubCommand: { } _})
                throw new NotImplementedException("opgfdhush ough no subcommands");

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


    public virtual bool TryGetReturnType(Type? pipedType, Type[] typeArguments, out Type? type)
    {
        var impls = GetConcreteImplementations(pipedType, typeArguments).ToList();

        if (impls.Count == 1)
        {
            type = impls.First().ReturnType;
            return true;
        }

        throw new NotImplementedException("write your own TryGetReturnType your command is too clamplicated.");
    }

    public List<MethodInfo> GetConcreteImplementations(Type? pipedType, Type[] typeArguments)
    {
        return GetGenericImplementations()
            .Select(x =>
        {
            if (x.IsGenericMethodDefinition)
            {
                if (x.HasCustomAttribute<TakesPipedTypeAsGeneric>())
                    return x.MakeGenericMethod(typeArguments.Append(pipedType!).ToArray());
                else
                    return x.MakeGenericMethod(typeArguments);
            }

            return x;
        }).ToList();
    }

    public List<MethodInfo> GetGenericImplementations()
    {
        var t = GetType();

        var methods = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance);

        return methods.Where(x => x.HasCustomAttribute<CommandImplementationAttribute>()).ToList();
    }

    public bool TryGetImplementation(Type? pipedType, string? subCommand, Type[] typeArguments, [NotNullWhen(true)] out Invocable? impl)
    {
        if (subCommand is not null)
        {
            throw new NotImplementedException("subcommands");
        }

        return Implementor.TryGetImplementation(pipedType, typeArguments, out impl);
    }

    public bool TryParseArguments(ForwardParser parser, [NotNullWhen(true)] out Dictionary<string, object?>? args, out Type[] resolvedTypeArguments)
    {
        if (Parameters is null)
            throw new NotImplementedException("dhfshbfghd");

        var output = new Dictionary<string, object?>();
        resolvedTypeArguments = new Type[TypeParameterParsers.Length];

        for (var i = 0; i < TypeParameterParsers.Length; i++)
        {
            if (!_newCon.TryParse(parser, TypeParameterParsers[i], out var parsed) || parsed is not IAsType ty)
            {
                Logger.Debug($"AWAWA {parsed} {TypeParameterParsers[i]}");
                resolvedTypeArguments = Array.Empty<Type>();
                args = null;
                return false;
            }

            resolvedTypeArguments[i] = ty.AsType();
        }

        foreach (var param in Parameters)
        {
            if (!_newCon.TryParse(parser, param.Value, out var parsed))
            {
                Logger.Debug("fuck");
                args = null;
                return false;
            }
            output[param.Key] = parsed;
        }

        args = output;
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

        if (!Owner.TryGetReturnType(pipedType, typeArguments, out var ty))
        {
            impl = null;
            return false;
        }

        if (Implementations.TryGetValue(discrim, out impl))
            return true;

        // Okay we need to build a new shim.

        var possibleImpls = Owner.GetConcreteImplementations(pipedType, typeArguments).Where(x => x.GetCustomAttribute<CommandImplementationAttribute>()?.SubCommand == SubCommand);

        IEnumerable<MethodInfo> impls;

        if (pipedType is null)
        {
            impls = possibleImpls.Where(x =>
                x.GetParameters().Any(param =>
                    param.GetCustomAttribute<PipedArgumentAttribute>() is not null && param.ParameterType.CanBeEmpty()
                    )
                || !x.GetParameters().Any(param => param.GetCustomAttribute<PipedArgumentAttribute>() is not null)
                || x.GetParameters().Length == 0);
        }
        else
        {
            impls = possibleImpls.Where(x =>
                x.GetParameters().Any(param =>
                    param.GetCustomAttribute<PipedArgumentAttribute>() is not null && pipedType.IsAssignableTo(param.ParameterType)
                ) || x.IsGenericMethodDefinition);
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

        }

        Expression partialShim = Expression.Call(Expression.Constant(Owner), unshimmed, paramList);

        if (ty is not null && ty.IsPrimitive)
            partialShim = Expression.Convert(partialShim, typeof(object)); // Have to box primitives.

        if (unshimmed.ReturnType == typeof(void))
            partialShim = Expression.Block(partialShim, Expression.Constant(null));

        var lambda = Expression.Lambda<Invocable>(partialShim, args);

        Implementations[discrim] = lambda.Compile();
        impl = Implementations[discrim];
        return true;
    }
}

public static class ReflectionExtensions
{
    public static bool CanBeNull(this Type t)
    {
        return !t.IsValueType || t.IsGenericType(typeof(Nullable<>));
    }

    public static bool CanBeEmpty(this Type t)
    {
        return t.CanBeNull() || t.IsGenericType(typeof(IEnumerable<>));
    }

    public static bool IsGenericType(this Type t, Type genericType)
    {
        return t.IsGenericType && t.GetGenericTypeDefinition() == genericType;
    }

    public static Expression CreateEmptyExpr(this Type t)
    {
        if (!t.CanBeEmpty())
            throw new TypeArgumentException();

        if (t.IsGenericType(typeof(IEnumerable<>)))
        {
            var array = Array.CreateInstance(t.GetGenericArguments().First(), 0);
            return Expression.Constant(array, t);
        }

        if (t.CanBeNull())
        {
            if (Nullable.GetUnderlyingType(t) is not null)
                return Expression.Constant(t.GetConstructor(BindingFlags.CreateInstance, Array.Empty<Type>())!.Invoke(null, null), t);

            return Expression.Constant(null, t);
        }

        throw new NotImplementedException();
    }

    public static PropertyInfo? FindIndexerProperty(
        this Type type)
    {
        var defaultPropertyAttribute = type.GetCustomAttributes<DefaultMemberAttribute>().FirstOrDefault();

        return defaultPropertyAttribute == null
            ? null
            : type.GetRuntimeProperties()
                .FirstOrDefault(
                    pi =>
                        pi.Name == defaultPropertyAttribute.MemberName
                        && pi.IsIndexerProperty()
                        && pi.SetMethod?.GetParameters() is { } parameters
                        && parameters.Length == 2
                        && parameters[0].ParameterType == typeof(string));
    }

    public static bool IsIndexerProperty(this PropertyInfo propertyInfo)
    {
        var indexParams = propertyInfo.GetIndexParameters();
        return indexParams.Length == 1
               && indexParams[0].ParameterType == typeof(string);
    }
}

public sealed class CommandInvocationArguments
{
    public required object? PipedArgument;
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
