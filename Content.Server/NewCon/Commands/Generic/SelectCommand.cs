using System.Collections;
using System.Linq;
using System.Reflection;
using Content.Server.NewCon.TypeParsers;
using Microsoft.Extensions.Logging;
using Robust.Shared.Random;

namespace Content.Server.NewCon.Commands.Generic;

[ConsoleCommand]
public sealed class SelectCommand : ConsoleCommand
{
    public override bool TryGetReturnType(string? subCommand, Type? pipedType, Type[] typeArguments, out Type? type)
    {
        if (pipedType is null || subCommand is not null)
        {
            type = null;
            return false;
        }

        if (pipedType.IsGenericType(typeof(IEnumerable<>)))
        {
            type = pipedType;
            return true;
        }

        type = null;
        return false;
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Select<T>([PipedArgument] T enumerable, [CommandArgument] Quantity quantity, [CommandInverted] bool inverted)
        where T: IEnumerable
    {
        // todo: cache this
        var castMethodGeneric = typeof(Enumerable).GetMethod("Cast", BindingFlags.Public | BindingFlags.Static)!;
        var castMethod = castMethodGeneric.MakeGenericMethod(typeof(T).GetGenericArguments()[0]);

        return (T)(IEnumerable)castMethod.Invoke(null, new [] {SelectInner(enumerable, quantity, inverted)})!;
    }

    private IEnumerable SelectInner(IEnumerable input, Quantity quantity, bool inverted)
    {
        var arr = input.Cast<object?>().OrderBy(_ => Guid.NewGuid()).ToArray();

        if (quantity is {Amount: { } amount})
        {
            var taken = (int) Math.Ceiling(amount);
            if (inverted)
                taken = Math.Max(0, arr.Length - taken);

            return arr.Take(taken);
        }
        else
        {
            var percent = inverted
                ? (int) Math.Floor(arr.Length * Math.Clamp(1 - (double) quantity.Percentage!, 0, 1))
                : (int) Math.Floor(arr.Length * Math.Clamp((double)  quantity.Percentage!, 0, 1));

            return arr.Take(percent);
        }
    }
}
