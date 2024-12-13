using System.Linq;
using Content.Server.Access.Systems;
using Content.Server.Administration.Logs;
using Content.Server.Kitchen.Components;
using Content.Server.NameIdentifier;
using Content.Shared._DeltaV.CartridgeLoader.Cartridges;
using Content.Shared._DeltaV.NanoChat;
using Content.Shared.Database;
using Content.Shared.NameIdentifier;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._DeltaV.NanoChat;

/// <summary>
///     Handles NanoChat features that are specific to the server but not related to the cartridge itself.
/// </summary>
public sealed class NanoChatSystem : SharedNanoChatSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly NameIdentifierSystem _name = default!;

    private readonly ProtoId<NameIdentifierGroupPrototype> _nameIdentifierGroup = "NanoChat";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NanoChatCardComponent, MapInitEvent>(OnCardInit);
        SubscribeLocalEvent<NanoChatCardComponent, BeingMicrowavedEvent>(OnMicrowaved, after: [typeof(IdCardSystem)]);
    }

    private void OnMicrowaved(Entity<NanoChatCardComponent> ent, ref BeingMicrowavedEvent args)
    {
        // Skip if the entity was deleted (e.g., by ID card system burning it)
        if (Deleted(ent))
            return;

        if (!TryComp<MicrowaveComponent>(args.Microwave, out var micro) || micro.Broken)
            return;

        var randomPick = _random.NextFloat();

        // Super lucky - erase all messages (10% chance)
        if (randomPick <= 0.10f)
        {
            ent.Comp.Messages.Clear();
            // TODO: these shouldn't be shown at the same time as the popups from IdCardSystem
            // _popup.PopupEntity(Loc.GetString("nanochat-card-microwave-erased", ("card", ent)),
            //     ent,
            //     PopupType.Medium);

            _adminLogger.Add(LogType.Action,
                LogImpact.Medium,
                $"{ToPrettyString(args.Microwave)} erased all messages on {ToPrettyString(ent)}");
        }
        else
        {
            // Scramble random messages for random recipients
            ScrambleMessages(ent);
            // _popup.PopupEntity(Loc.GetString("nanochat-card-microwave-scrambled", ("card", ent)),
            //     ent,
            //     PopupType.Medium);

            _adminLogger.Add(LogType.Action,
                LogImpact.Medium,
                $"{ToPrettyString(args.Microwave)} scrambled messages on {ToPrettyString(ent)}");
        }

        Dirty(ent);
    }

    private void ScrambleMessages(NanoChatCardComponent component)
    {
        foreach (var (recipientNumber, messages) in component.Messages)
        {
            for (var i = 0; i < messages.Count; i++)
            {
                // 50% chance to scramble each message
                if (!_random.Prob(0.5f))
                    continue;

                var message = messages[i];
                message.Content = ScrambleText(message.Content);
                messages[i] = message;
            }

            // 25% chance to reassign the conversation to a random recipient
            if (_random.Prob(0.25f) && component.Recipients.Count > 0)
            {
                var newRecipient = _random.Pick(component.Recipients.Keys.ToList());
                if (newRecipient == recipientNumber)
                    continue;

                if (!component.Messages.ContainsKey(newRecipient))
                    component.Messages[newRecipient] = new List<NanoChatMessage>();

                component.Messages[newRecipient].AddRange(messages);
                component.Messages[recipientNumber].Clear();
            }
        }
    }

    private string ScrambleText(string text)
    {
        var chars = text.ToCharArray();
        var n = chars.Length;

        // Fisher-Yates shuffle of characters
        while (n > 1)
        {
            n--;
            var k = _random.Next(n + 1);
            (chars[k], chars[n]) = (chars[n], chars[k]);
        }

        return new string(chars);
    }

    private void OnCardInit(Entity<NanoChatCardComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Number != null)
            return;

        // Assign a random number
        _name.GenerateUniqueName(ent, _nameIdentifierGroup, out var number);
        ent.Comp.Number = (uint)number;
        Dirty(ent);
    }
}
