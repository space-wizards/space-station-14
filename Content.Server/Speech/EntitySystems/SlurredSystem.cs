using System.Text;
using Content.Server.Speech.Components;
using Content.Shared.Drunk;
using Content.Shared.Speech;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Speech.EntitySystems;

public sealed class SlurredSystem : SharedSlurredSystem
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SlurredAccentComponent, AccentGetEvent>(OnAccent);

        SubscribeLocalEvent<SlurredAccentComponent, StatusEffectRelayedEvent<AccentGetEvent>>(OnAccentRelayed);
    }

    /// <summary>
    ///     Slur chance scales with the time remaining on any status effect with the SlurredAccentComponent.
    ///     Typically, this is equivalent to "drunkenness" on the DrunkStatusEffect
    /// </summary>
    private float GetProbabilityScale(EntityUid uid)
    {
        if (!_status.TryGetMaxTime<SlurredAccentComponent>(uid, out var time))
            return 0;

        // This is a magic number. Why this value? No clue it was made 3 years before I refactored this.
        var magic = SharedDrunkSystem.MagicNumber;

        if (time.Item2 != null)
        {
            var curTime = _timing.CurTime;
            magic = (float) (time.Item2 - curTime).Value.TotalSeconds - 80f;
        }

        return Math.Clamp(magic / SharedDrunkSystem.MagicNumber, 0f, 1f);
    }

    private void OnAccent(Entity<SlurredAccentComponent> entity, ref AccentGetEvent args)
    {
        GetAccent(entity, ref args);
    }

    private void OnAccentRelayed(Entity<SlurredAccentComponent> entity, ref StatusEffectRelayedEvent<AccentGetEvent> args)
    {
        var ev = args.Args;
        GetAccent(args.Args.Entity, ref ev);
    }

    private void GetAccent(EntityUid uid, ref AccentGetEvent args)
    {
        var scale = GetProbabilityScale(uid);
        args.Message = Accentuate(args.Message, scale);
    }

    private string Accentuate(string message, float scale)
    {
        var sb = new StringBuilder();

        // This is pretty much ported from TG.
        foreach (var character in message)
        {
            var modifiedCharacter = character;

            if (_random.Prob(scale / 3f))
            {
                var lower = char.ToLowerInvariant(modifiedCharacter);
                var newString = lower switch
                {
                    'o' => "u",
                    's' => "ch",
                    'a' => "ah",
                    'u' => "oo",
                    'c' => "k",
                    _ => $"{modifiedCharacter}",
                };

                sb.Append(newString);
            }

            if (_random.Prob(scale / 20f))
            {
                switch (modifiedCharacter)
                {
                    case ' ':
                        sb.Append(Loc.GetString("slur-accent-confused"));
                        break;
                
                    case '.':
                        sb.Append(' ');
                        sb.Append(Loc.GetString("slur-accent-burp"));
                        break;
                }    
            }

            // Change lowercase characters to uppercase and vice versa
            if (_random.Prob(scale / 3f))
            {
                if (char.IsLower(modifiedCharacter))
                {
                    modifiedCharacter = char.ToUpperInvariant(modifiedCharacter);
                }
                else if (char.IsUpper(modifiedCharacter))
                {
                    modifiedCharacter = char.ToLowerInvariant(modifiedCharacter);
                }
            }


            if (!_random.Prob(scale * 3/20))
            {
                sb.Append(modifiedCharacter);
                continue;
            }

            var next = _random.Next(1, 3) switch
            {
                1 => "'",
                2 => $"{modifiedCharacter}{modifiedCharacter}",
                _ => $"{modifiedCharacter}{modifiedCharacter}{modifiedCharacter}",
            };

            sb.Append(next);
        }

        return sb.ToString();
    }
}
