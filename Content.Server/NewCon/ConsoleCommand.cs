using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Robust.Shared.Exceptions;
using Robust.Shared.Utility;

using Invocable = System.Func<Content.Server.NewCon.CommandInvocationArguments, object?>;

namespace Content.Server.NewCon;

public abstract class ConsoleCommand
{
    public bool HasSubCommands { get; init; }

    private ConsoleCommandImplementor Implementor;

    public ConsoleCommand()
    {
        Implementor =
            new ConsoleCommandImplementor
            {
                Owner = this,
                SubCommand = null
            };
    }

    public abstract bool TryGetReturnType(Type? pipedType, out Type? type);

    public List<MethodInfo> GetUnwrappedImplementations()
    {
        var t = GetType();
        var methods = t.GetMethods(BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

        return methods.Where(x => x.HasCustomAttribute<CommandImplementationAttribute>()).ToList();
    }

    public bool TryGetImplementation(Type? pipedType, string? subCommand, [NotNullWhen(true)] out Invocable? impl)
    {
        if (subCommand is not null)
        {
            throw new NotImplementedException("subcommands");
        }

        return Implementor.TryGetImplementation(pipedType, out impl);
    }
}

public sealed class ConsoleCommandImplementor
{
    public required ConsoleCommand Owner;

    public required string? SubCommand;

    public Dictionary<Type, Invocable> TypeImplementations = new();

    public Invocable? UntypedImplementation = null;

    public bool TryGetImplementation(Type? pipedType, [NotNullWhen(true)] out Invocable? impl)
    {
        if (!Owner.TryGetReturnType(pipedType, out var ty))
        {
            impl = null;
            return false;
        }

        if (ty is null)
        {
            impl = UntypedImplementation;
            if (impl is not null)
                return true;
        }
        else
        {
            if (TypeImplementations.TryGetValue(ty, out impl))
                return true;
        }

        // Okay we need to build a new shim.

        var possibleImpls = Owner.GetUnwrappedImplementations().Where(x => x.GetCustomAttribute<CommandImplementationAttribute>()?.SubCommand == SubCommand);

        // untypedEnumerable.MakeGenericType USE THIS

        if (pipedType is null)
        {
            var impls = possibleImpls.Where(x =>
                x.GetParameters().Any(param =>
                    param.GetCustomAttribute<PipedArgumentAttribute>() is not null && param.ParameterType.CanBeEmpty()
                    )
                || !x.GetParameters().Any(param => param.GetCustomAttribute<PipedArgumentAttribute>() is not null));

            var unshimmed = impls.First();

            var args = Expression.Parameter(typeof(CommandInvocationArguments));

            var paramList = new List<Expression>();

            foreach (var param in unshimmed.GetParameters())
            {
                if (param.GetCustomAttribute<PipedArgumentAttribute>() is { } _)
                {
                    paramList.Add(param.ParameterType.CreateEmptyExpr());
                    continue;
                }

                if (param.GetCustomAttribute<CommandArgumentAttribute>() is { } arg)
                {
                    // (ParameterType)(args.Arguments[param.Name])
                    paramList.Add(Expression.Convert(
                        Expression.ArrayIndex(
                            Expression.Field(args, nameof(CommandInvocationArguments.Arguments)),
                            Expression.Constant(param.Name)
                            ),
                        param.ParameterType));
                    continue;
                }
            }

            var partialShim = Expression.Call(Expression.Constant(unshimmed), unshimmed, paramList);

            UntypedImplementation = Expression.Lambda<Invocable>(partialShim, args).Compile();

            impl = UntypedImplementation;

            return true;
        }

        throw new NotImplementedException("ahdfgsgcdfvgtweytjfwbgne AWAWAWAWA");
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
        if (t.CanBeEmpty())
            throw new TypeArgumentException();

        if (t.CanBeNull())
        {
            if (Nullable.GetUnderlyingType(t) is not null)
                return Expression.Constant(t.GetConstructor(BindingFlags.CreateInstance, Array.Empty<Type>())!.Invoke(null, null), t);

            return Expression.Constant(null, t);
        }

        if (t.IsGenericType(typeof(IEnumerable<>)))
        {
            var array = Array.CreateInstance(t.GetGenericArguments().First(), 0);
            return Expression.Constant(array, t);
        }

        throw new NotImplementedException();
    }
}

public sealed class CommandInvocationArguments
{
    public required object? PipedArgument;
    public required Dictionary<string, object?> Arguments;
}
