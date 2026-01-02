using Content.Server.Chat.Systems;
using Content.Server.Vocalization.Components;
using Content.Shared.Chat;
using Content.Shared.Inventory;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Vocalization.Systems;

/// <summary>
/// RadioVocalizationSystem handles vocalizing things via equipped radios when a VocalizeEvent is fired
/// </summary>
public sealed partial class RadioVocalizationSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadioVocalizerComponent, VocalizeEvent>(OnVocalize);
    }

    /// <summary>
    /// Called whenever an entity with a VocalizerComponent tries to speak
    /// </summary>
    private void OnVocalize(Entity<RadioVocalizerComponent> entity, ref VocalizeEvent args)
    {
        if (args.Handled)
            return;

        // set to handled if we succeed in speaking on the radio
        args.Handled = TrySpeakRadio(entity.Owner, args.Message);
    }

    /// <summary>
    /// Selects a random radio channel from all ActiveRadio entities in a given entity's inventory
    /// If no channels are found, this returns false and sets channel to an empty string
    /// </summary>
    private bool TryPickRandomRadioChannel(EntityUid entity, out ProtoId<RadioChannelPrototype> channel)
    {
        HashSet<ProtoId<RadioChannelPrototype>> potentialChannels = [];

        // we don't have to check if this entity has an inventory. GetHandOrInventoryEntities will not yield anything
        // if an entity has no inventory or inventory slots
        foreach (var item in _inventory.GetHandOrInventoryEntities(entity))
        {
            if (!TryComp<ActiveRadioComponent>(item, out var radio))
                continue;

            potentialChannels.UnionWith(radio.Channels);
        }

        if (potentialChannels.Count == 0)
        {
            channel = string.Empty;
            return false;
        }

        channel = _random.Pick(potentialChannels);

        return true;
    }

    /// <summary>
    /// Attempts to speak on the radio. Returns false if there is no radio or talking on radio fails somehow
    /// </summary>
    /// <param name="entity">Entity to try and make speak on the radio</param>
    /// <param name="message">Message to speak</param>
    private bool TrySpeakRadio(Entity<RadioVocalizerComponent?> entity, string message)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        if (!_random.Prob(entity.Comp.RadioAttemptChance))
            return false;

        if (!TryPickRandomRadioChannel(entity, out var channel))
            return false;

        var channelPrefix = _proto.Index<RadioChannelPrototype>(channel).KeyCode;

        // send a whisper using the radio channel prefix and whatever relevant radio channel character
        // along with the message. This is analogous to how radio messages are sent by players
        _chat.TrySendInGameICMessage(
            entity,
            $"{SharedChatSystem.RadioChannelPrefix}{channelPrefix} {message}",
            InGameICChatType.Whisper,
            ChatTransmitRange.Normal);

        return true;
    }
}
