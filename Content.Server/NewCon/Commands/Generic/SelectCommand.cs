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
    [Dependency] private readonly IRobustRandom _random = default!;

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<TR> Select<TR>([PipedArgument] IEnumerable<TR> enumerable, [CommandArgument] Quantity quantity, [CommandInverted] bool inverted)
    {
        var arr = enumerable.ToArray();
        _random.Shuffle(arr);

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
