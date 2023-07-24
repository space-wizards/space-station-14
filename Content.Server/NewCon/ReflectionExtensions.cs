using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Robust.Shared.Exceptions;
using Robust.Shared.Utility;

namespace Content.Server.NewCon;

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

    public static IEnumerable<Type> GetVariants(this Type t, NewConManager newCon)
    {
        var args = t.GetGenericArguments();
        var generic = t.GetGenericTypeDefinition();
        var genericArgs = generic.GetGenericArguments();
        var variantCount = genericArgs.Count(x => (x.GenericParameterAttributes & GenericParameterAttributes.VarianceMask) != 0);

        if (variantCount > 1)
        {
            throw new NotImplementedException("I swear to god I am NOT supporting more than one variant type parameter.");
        }

        yield return t;

        if (variantCount < 1)
        {
            yield break;
        }

        var variant = 0;
        for (var i = 0; i < args.Length; i++)
        {
            if ((genericArgs[i].GenericParameterAttributes & GenericParameterAttributes.VarianceMask) != 0)
            {
                variant = i;
                break;
            }
        }

        var newArgs = (Type[]) args.Clone();

        foreach (var type in newCon.AllSteppedTypes(args[variant], false))
        {
            newArgs[variant] = type;
            yield return generic.MakeGenericType(newArgs);
        }
    }

    public static IEnumerable<(string, List<MethodInfo>)> BySubCommand(this IEnumerable<MethodInfo> methods)
    {
        var output = new Dictionary<string, List<MethodInfo>>();

        foreach (var method in methods)
        {
            var subCommand = method.GetCustomAttribute<CommandImplementationAttribute>()!.SubCommand ?? "";
            if (!output.TryGetValue(subCommand, out var methodList))
            {
                methodList = new();
                output[subCommand] = methodList;
            }
            methodList.Add(method);
        }

        return output.Select(x => (x.Key, x.Value));
    }

    public static Type StepDownConstraints(this Type t)
    {
        if (!t.IsGenericType)
            return t;

        var oldArgs = t.GenericTypeArguments;
        var newArgs = new Type[oldArgs.Length];

        for (var i = 0; i < oldArgs.Length; i++)
        {
            if (oldArgs[i].IsGenericType)
                newArgs[i] = oldArgs[i].GetGenericTypeDefinition();
            else
                newArgs[i] = oldArgs[i];
        }

        return t.GetGenericTypeDefinition().MakeGenericType(newArgs);
    }

    public static string PrettyName(this Type type)
    {
        var name = type.Name;

        if (type.IsGenericParameter)
            return type.ToString();

        if (type.DeclaringType is not null)
        {
            name = $"{PrettyName(type.DeclaringType!)}.{type.Name}";
        }

        if (type.GetGenericArguments().Length == 0)
        {
            return name;
        }

        if (!name.Contains('`'))
            return name + "<>";

        var genericArguments = type.GetGenericArguments();
        var exactName = name.Substring(0, name.IndexOf('`', StringComparison.InvariantCulture));
        return exactName + "<" + string.Join(",", genericArguments.Select(PrettyName)) + ">";
    }

    public static ParameterInfo? ConsoleGetPipedArgument(this MethodInfo method)
    {
        var p = method.GetParameters().Where(x => x.GetCustomAttribute<PipedArgumentAttribute>() is not null).ToList();
        return p.FirstOrDefault();
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

    // IEnumerable<EntityUid> ^ IEnumerable<T> -> EntityUid
    public static Type Intersect(this Type left, Type right)
    {
        if (!left.IsGenericType)
            return left;

        if (!right.IsGenericType)
            return left;

        return left.GetGenericArguments().First();
    }

    public static void DumpGenericInfo(this Type t)
    {
        Logger.Debug($"Info for {t.PrettyName()}");
        Logger.Debug(
            $"GP {t.IsGenericParameter} | MP {t.IsGenericMethodParameter} | TP {t.IsGenericTypeParameter} | DEF {t.IsGenericTypeDefinition} | TY {t.IsGenericType} | CON {t.IsConstructedGenericType}");
        if (t.IsGenericParameter)
            Logger.Debug($"CONSTRAINTS: {string.Join(", ", t.GetGenericParameterConstraints().Select(PrettyName))}");
        if (!t.IsGenericTypeDefinition && IsGenericRelated(t) && t.IsGenericType)
            DumpGenericInfo(t.GetGenericTypeDefinition());
        foreach (var p in t.GetGenericArguments())
        {
            DumpGenericInfo(p);
        }
    }



    public static bool IsAssignableToGeneric(this Type left, Type right)
    {
        if (left.IsAssignableTo(right))
            return true;

        if (left.Constructable() &&
            (right.IsGenericParameter || right.IsGenericTypeParameter || right.IsGenericMethodParameter))
        {
            // TODO: constraint evaluation.
            return true;
        }

        if (left.IsGenericType && right.IsGenericType && left.GenericTypeArguments.Length == right.GenericTypeArguments.Length)
        {
            var equal = left.GetGenericTypeDefinition() == right.GetGenericTypeDefinition();

            if (!equal)
                return false;

            var res = true;
            foreach (var (leftTy, rightTy) in left.GenericTypeArguments.Zip(right.GenericTypeArguments))
            {
                res &= leftTy.IsAssignableToGeneric(rightTy);
            }

            return res;
        }

        return false;
    }

    public static bool IsGenericRelated(this Type t)
    {
        return t.IsGenericParameter | t.IsGenericType | t.IsGenericMethodParameter | t.IsGenericTypeDefinition | t.IsConstructedGenericType | t.IsGenericTypeParameter;
    }

    public static bool Constructable(this Type t)
    {
        if (!IsGenericRelated(t))
            return true;

        if (!t.IsGenericType)
            return false;

        var r = true;

        foreach (var arg in t.GetGenericArguments())
        {
            r &= Constructable(arg);
        }

        return r;
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
