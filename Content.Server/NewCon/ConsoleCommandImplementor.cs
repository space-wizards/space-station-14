using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Content.Server.NewCon.Errors;
using Content.Server.NewCon.TypeParsers;

namespace Content.Server.NewCon;

internal sealed class ConsoleCommandImplementor
{
    [Dependency] private readonly NewConManager _newConManager = default!;
    public required ConsoleCommand Owner;

    public required string? SubCommand;

    public Dictionary<CommandDiscriminator, Func<CommandInvocationArguments, object?>> Implementations = new();

    public ConsoleCommandImplementor()
    {
        IoCManager.InjectDependencies(this);
    }

    public bool TryParseArguments(ForwardParser parser, string? subCommand, Type? pipedType, [NotNullWhen(true)] out Dictionary<string, object?>? args, out Type[] resolvedTypeArguments, out IConError? error)
    {
        resolvedTypeArguments = new Type[Owner.TypeParameterParsers.Length];

        for (var i = 0; i < Owner.TypeParameterParsers.Length; i++)
        {
            var start = parser.Index;
            if (!_newConManager.TryParse(parser, Owner.TypeParameterParsers[i], out var parsed, out error) || parsed is not { } ty)
            {
                error?.Contextualize(parser.Input, (start, parser.Index));
                resolvedTypeArguments = Array.Empty<Type>();
                args = null;
                return false;
            }

            Type real;
            if (ty is IAsType<Type> asTy)
            {
                real = asTy.AsType();
            }
            else if (ty is Type realTy)
            {
                real = realTy;
            }
            else
            {
                throw new NotImplementedException();
            }

            resolvedTypeArguments[i] = real;
        }

        var impls = Owner.GetConcreteImplementations(pipedType, resolvedTypeArguments, subCommand);
        if (impls.FirstOrDefault() is not { } impl)
        {
            args = null;
            error = null;
            return false;
        }

        args = new();

        foreach (var argument in impl.ConsoleGetArguments())
        {
            var start = parser.Index;
            if (!_newConManager.TryParse(parser, argument.ParameterType, out var parsed, out error))
            {
                error?.Contextualize(parser.Input, (start, parser.Index));
                args = null;
                return false;
            }
            args[argument.Name!] = parsed;
        }

        error = null;
        return true;
    }

    public bool TryGetImplementation(Type? pipedType, Type[] typeArguments, [NotNullWhen(true)] out Func<CommandInvocationArguments, object?>? impl)
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

        var lambda = Expression.Lambda<Func<CommandInvocationArguments, object?>>(partialShim, args);

        Implementations[discrim] = lambda.Compile();
        impl = Implementations[discrim];
        return true;
    }
}
