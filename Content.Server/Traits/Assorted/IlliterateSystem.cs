using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.Arcade;
using Robust.Shared.Random;
using static Content.Shared.Paper.SharedPaperComponent;

namespace Content.Server.Traits.Assorted;

public sealed class IlliterateSystem : EntitySystem
{

    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WriteAttemptEvent>(OnWriteAttempt);
        SubscribeLocalEvent<ReadAttemptEvent>(OnReadAttempt);
    }

    private void OnWriteAttempt(WriteAttemptEvent ev)
    {
        if (IsIlliterate(ev.Writer))
            ev.CanWrite = false;
    }

    private void OnReadAttempt(ReadAttemptEvent ev)
    {
        if (IsIlliterate(ev.Reader))
            ev.CanRead = false;
    }

    public string ScrambleString(string str)
    {
        var sb = new StringBuilder();

        foreach (var character in str)
        {
            //If this is a letter or number, replace it with a random character
            if (char.IsLetterOrDigit(character))
            {
                sb.Append((char) (97 + _random.Next(0, 26)));
            }
            else
            {
                //Otherwise just pass it, this is to allow drawings
                sb.Append(character);
            }
        }

        return sb.ToString();
    }

    private bool IsIlliterate(EntityUid? entity)
    {
        if (!entity.HasValue)
            return false;

        return HasComp<IlliterateComponent>(entity);
    }
}
