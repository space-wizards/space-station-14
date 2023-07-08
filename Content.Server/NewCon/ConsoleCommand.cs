using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using Robust.Shared.Exceptions;
using Robust.Shared.Utility;

using Invocable = System.Func<Content.Server.NewCon.CommandInvocationArguments, object?>;

namespace Content.Server.NewCon;

public abstract class ConsoleCommand
{
    [Dependency] private readonly NewConManager _newCon = default!;

    public bool HasSubCommands { get; init; }

    public readonly SortedDictionary<string, Type>? Parameters;

    private ConsoleCommandImplementor Implementor;

    public ConsoleCommand()
    {
        Implementor =
            new ConsoleCommandImplementor
            {
                Owner = this,
                SubCommand = null
            };

        var impls = GetUnwrappedImplementations();

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


    public virtual bool TryGetReturnType(Type? pipedType, out Type? type)
    {
        var impls = GetUnwrappedImplementations().ToList();

        if (impls.Count == 1)
        {
            type = impls.First().ReturnType;
            return true;
        }

        type = null;
        return false;

        throw new NotImplementedException("write your own TryGetReturnType your command is too clamplicated.");
    }

    public List<MethodInfo> GetUnwrappedImplementations()
    {
        var t = GetType();

        var methods = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance);

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

    public Dictionary<string, object?> ParseArguments(ForwardParser parser)
    {
        if (Parameters is null)
            throw new NotImplementedException("dhfshbfghd");

        var output = new Dictionary<string, object?>();

        foreach (var param in Parameters)
        {
            _newCon.TryParse(parser, param.Value, out var parsed);
            output[param.Key] = parsed;
        }

        return output;
    }
}

public sealed class ConsoleCommandImplementor
{
    public required ConsoleCommand Owner;

    public required string? SubCommand;

    public Dictionary<Type, Invocable> TypeImplementations = new();

    public Invocable? UntypedImplementation = null;

    public MethodInfo? UntypedMethod = null;

    public bool TryGetImplementation(Type? pipedType, [NotNullWhen(true)] out Invocable? impl)
    {
        if (!Owner.TryGetReturnType(pipedType, out var ty))
        {
            impl = null;
            return false;
        }

        if (pipedType is null)
        {
            impl = UntypedImplementation;
            if (impl is not null)
                return true;
        }
        else
        {
            if (TypeImplementations.TryGetValue(pipedType, out impl))
                return true;
        }

        // Okay we need to build a new shim.

        var possibleImpls = Owner.GetUnwrappedImplementations().Where(x => x.GetCustomAttribute<CommandImplementationAttribute>()?.SubCommand == SubCommand);

        // untypedEnumerable.MakeGenericType USE THIS

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
            Logger.Error("Found zero potential implementations.");
            return false;
        }

        var unshimmed = implArray.First();

        if (unshimmed.IsGenericMethodDefinition)
            unshimmed = unshimmed.MakeGenericMethod(pipedType!);


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
                        Expression.Field(args, nameof(CommandInvocationArguments.Arguments)),
                        typeof(Dictionary<string, object?>).FindIndexerProperty(),
                        new [] {Expression.Constant(param.Name)}),
                param.ParameterType));
                continue;
            }

            if (param.GetCustomAttribute<CommandInvertedAttribute>() is { } _)
            {
                // args.Inverted
                paramList.Add(Expression.Field(args, nameof(CommandInvocationArguments.Inverted)));
                continue;
            }

        }

        Expression partialShim = Expression.Call(Expression.Constant(Owner), unshimmed, paramList);

        if (ty is not null && ty.IsPrimitive)
            partialShim = Expression.Convert(partialShim, typeof(object)); // Have to box primitives.

        if (unshimmed.ReturnType == typeof(void))
            partialShim = Expression.Block(partialShim, Expression.Constant(null));

        var lambda = Expression.Lambda<Invocable>(partialShim, args);

        if (pipedType is not null)
        {
            TypeImplementations[pipedType] = lambda.Compile();
            impl = TypeImplementations[pipedType];
            return true;
        }
        else
        {
            UntypedImplementation = lambda.Compile();
            impl = UntypedImplementation;
            UntypedMethod = unshimmed;
            return true;
        }
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
    public required Dictionary<string, object?> Arguments;
    public required bool Inverted = false;
    public required Type? PipedArgumentType;
}
