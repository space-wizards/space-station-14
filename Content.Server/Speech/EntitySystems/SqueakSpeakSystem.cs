using System.Text;
using Content.Shared.Speech.Components;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffect;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class SqueakSpeakSystem : SharedSqueakSpeakSystem
{
    //I used the RatvarianLanguageSystem as a basis for how to make this.
    //Inserts hiccups and squeaks (and sometimes both!) into whoever is afflicted.
    //Originally intended for use with the fried fisher boots.
    //It can be used for other things, but I don't know why you would.
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    private const string SqueakSpeakKey = "SqueakSpeak";

    public override void Initialize()
    {
        // Activate before other modifications so squeakification works properly
        SubscribeLocalEvent<SqueakSpeakComponent, AccentGetEvent>(OnAccent, before: new[] { typeof(SharedSlurredSystem), typeof(SharedStutteringSystem) });
    }
    public override void DoSqueakSpeak(EntityUid uid, TimeSpan time, bool refresh, StatusEffectsComponent? status = null)
    {
        if (!Resolve(uid, ref status, false))
            return;

        _statusEffects.TryAddStatusEffect<SqueakSpeakComponent>(uid, SqueakSpeakKey, time, refresh, status);
    }

    private void OnAccent(EntityUid uid, SqueakSpeakComponent component, AccentGetEvent args)
    {
        args.Message = Squeakify(args.Message);
    }

    //Actually does the squeaking.
    //Simple algorithm. Checks the length of the word to see if it can insert a squeak.
    //Runs a random check to insert a squeak. Then, if a squeak hasn't been inserted on that
    //word, runs a random check to add a squeak to the end.
    //Also does a silly little random check to decide between type of squeak.
    private string Squeakify(string message)
    {
        var postSqueak = new StringBuilder();
        var toBeSqueaked = message.Split(' ');
        var squeakRandom = 0;
        string[] squeakChoice = {"-hicsqueak-", "-squeak-", "-hic-"};

        foreach (var word in toBeSqueaked)
        {
            var temp = word;
            if (word.Length >= 6)
            {
                squeakRandom = _random.Next(10);
                if (squeakRandom > 3)
                {
                    temp.Insert(_random.Next(2, word.Length - 3), squeakChoice[_random.Next(3)]);
                }

            }

            if (squeakRandom <= 3)
            {
                squeakRandom = _random.Next(10);
                if (squeakRandom > 3)
                {
                    temp = temp + " " + squeakChoice[_random.Next(3)];
                }
            }

            postSqueak.Append(temp + " ");
            squeakRandom = 0;
        }

        return postSqueak.ToString().Trim();
    }
}
