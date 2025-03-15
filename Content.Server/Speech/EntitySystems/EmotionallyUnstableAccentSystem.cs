using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Server.Chat.Systems;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class EmotionallyUnstableAccentSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly Regex DotRegex = new Regex(@"(?<!\.)\.(?=\s|$)");
    private static readonly Regex ExclamationMarksRegex = new Regex(@"!+");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmotionallyUnstableAccentComponent, AccentGetEvent>(OnAccent);
    }

    private string Accentuate(EmotionallyUnstableAccentComponent component, string msg)
    {
        var message = msg;

        // This condition determines whether we will modify the message or not.
        if (_random.Prob(component.ChangeMessageChance))
        {
            // This condition determines the replacement characters: with a certain chance specified in the component,
            // the sentence will either become exclamatory or contemplative (exclamation marks will be replaced with ellipses).
            if (_random.Prob(component.ExclamatorySentenceChance))
            {
                // . => !
                message = DotRegex.Replace(message, "!");

                // If the last character in the message is not a '?', append a '!' at the end.
                // This check must be performed before replacing '?', to avoid double exclamation marks in interrogative sentences.
                if (message[message.Length - 1] != '?')
                    message += "!";

                // ? => ?!
                message = message.Replace("?", "?!");
            }
            else
            {
                // . => ...
                message = DotRegex.Replace(message, "...");

                // ! => ...
                message = ExclamationMarksRegex.Replace(message, "...");

                // If the last character in the message is neither '?' nor '.', append '...' to the end.
                if (message[message.Length - 1] != '?' && message[message.Length - 1] != '.')
                    message += "...";
            }
        }
        return message;
    }
    private void OnAccent(Entity<EmotionallyUnstableAccentComponent> entity, ref AccentGetEvent args)
    {
        args.Message = Accentuate(entity.Comp, args.Message);

        if (entity.Comp.Emotes.Count == 0)
            return;

        // This condition determines whether the emotion will be played after the phrase.
        if (_random.Prob(entity.Comp.TriggerEmotionChance))
        {
            // Play the emotion by random index.
            _chat.TryEmoteWithChat(entity, entity.Comp.Emotes[_random.Next(0, entity.Comp.Emotes.Count)], ignoreActionBlocker: false);
        }
    }
}
