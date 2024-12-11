using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Server.Chat.Systems;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class BipolarDisorderAccentSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly Regex RegexDots = new Regex(@"\.{1,3}");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BipolarDisorderAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, BipolarDisorderAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // This condition determines whether we will modify the message or not.
        if (_random.Prob(component.ChangeMessageChance))
        {
            /* This condition determines the replacement characters: with a 50% chance,
               the sentence will either become exclamatory or contemplative (exclamation marks will be replaced with ellipses). */
            if (_random.Prob(0.5f))
            {
                // . => !
                message = message.Replace(".", "!");

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
                message = RegexDots.Replace(message, "...");

                // ! => ...
                message = message.Replace("!", "...");

                // If the last character in the message is neither '?' nor '.', append '...' to the end.
                if (message[message.Length - 1] != '?' && message[message.Length - 1] != '.')
                    message += "...";
            }
        }

        // This condition determines whether we will laugh after the phrase.
        if (_random.Prob(component.TriggerEmotionChance))
        {
            // The next emotion to play is selected by randomly generating the index of an emotion.
            component.NextEmotionIndex = _random.Next(0, component.Emotions.Count);

            // Play the emotion by index "NextEmotionIndex".
            _chat.TryEmoteWithChat(uid, component.Emotions[component.NextEmotionIndex], ignoreActionBlocker: false);
        }

        args.Message = message;
    }
}
