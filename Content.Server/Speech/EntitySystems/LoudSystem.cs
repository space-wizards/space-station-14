using Content.Server.Speech.Components;
using Robust.Shared.Random;
using System.Text.RegularExpressions;

namespace Content.Server.Speech.EntitySystems;

public sealed class LoudSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LoudAccentComponent, AccentGetEvent>(OnAccentGet);
    }
    public string Accentuate(string message, LoudAccentComponent component)
    {

        var msg = message;

        //Makes everything you say caps

        msg = msg.ToString().ToUpper();

        //Appends !! so that you yell and chat is bold
        msg = msg + "!!";

        //Get .ftl file with suffixes
        msg = _replacement.ApplyReplacements(msg, "loud");

        //Apply random suffix to var if random chance succeeds
        if (!_random.Prob(component.YellChance))
        {
            var pick = _random.Pick(component.YellSuffixes);

            msg = msg + " " + Loc.GetString(pick);
            return msg;
        }



 
        return msg;
    }

    private void OnAccentGet(EntityUid uid, LoudAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
