using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Server.Chat.Systems;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class HypomaniaAccentSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HypomaniaAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, HypomaniaAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // . => !
        message = message.Replace(".", "!");

        /// <summary>
        /// If the last character in the message is not a '?', append a '!' at the end.
        /// This check must be performed before replacing '?', to avoid double exclamation marks in interrogative sentences.
        /// <summary>
        if (message[message.Length - 1] != '?')
            message += "!";

        // ? => ?!
        message = message.Replace("?", "?!");


        // This condition determines whether we will laugh after the phrase.
        if (_random.Prob(component.LaughChance))
        {
            // Play the emotion of laughter.
            _chat.TryEmoteWithChat(uid, "Laugh", ignoreActionBlocker: false);
        }

        args.Message = message;
    }
}
