using Content.Server.Chat.Systems;
using Content.Server.Radio.Components;
using Content.Server.Vocalization.Components;
using Content.Shared.Chat;
using Content.Shared.Clothing;
using Content.Shared.Radio;
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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadioVocalizerComponent, ClothingDidEquippedEvent>(OnClothingEquipped);
        SubscribeLocalEvent<RadioVocalizerComponent, ClothingDidUnequippedEvent>(OnClothingUnequipped);

        SubscribeLocalEvent<RadioVocalizerComponent, MapInitEvent>(RadioOnMapInit);

        SubscribeLocalEvent<RadioVocalizerComponent, VocalizeEvent>(OnVocalize);
    }

    /// <summary>
    /// Callback for when this entity equips clothing or has clothing equipped by something
    /// This updates the list of entities with ActiveRadioComponents that are used to set radio channels
    /// </summary>
    private void OnClothingEquipped(Entity<RadioVocalizerComponent> entity, ref ClothingDidEquippedEvent args)
    {
        // return if this entity does not have an ActiveRadioComponent
        if (!TryComp<ActiveRadioComponent>(entity, out var activeRadio))
            return;

        // only care if the equipped clothing item has an ActiveRadioComponent
        if (!HasComp<ActiveRadioComponent>(args.Clothing))
            return;

        entity.Comp.ActiveRadioEntities.Add(args.Clothing.Owner);

        // update active radio channels
        UpdateRadioChannels((entity, activeRadio, entity));
    }

    /// <summary>
    /// Called if this entity unequipped clothing or has clothing unequipped by something
    /// This updates the list of entities with ActiveRadioComponents that are used to set radio channels
    /// </summary>
    private void OnClothingUnequipped(Entity<RadioVocalizerComponent> entity, ref ClothingDidUnequippedEvent args)
    {
        // return if this entity does not have an ActiveRadioComponent
        if (!TryComp<ActiveRadioComponent>(entity, out var activeRadio))
            return;

        // try to remove this item from the active radio entities list
        // if this returns false, the item wasn't found so it was never a radio we cared about, quit early
        if (!entity.Comp.ActiveRadioEntities.Remove(args.Clothing.Owner))
            return;

        // update active radio channels
        UpdateRadioChannels((entity, activeRadio, entity));
    }

    /// <summary>
    /// Called when the map is initialized (or after an entity is created).
    /// This ensures radio channels are updated when an entity spawns in with a radio pre-equipped
    /// </summary>
    private void RadioOnMapInit(Entity<RadioVocalizerComponent> entity, ref MapInitEvent args)
    {
        // If an entity has a VocalizerRadioComponent it really ought to have an ActiveRadioComponent
        var activeRadio = EnsureComp<ActiveRadioComponent>(entity);

        UpdateRadioChannels((entity, activeRadio, entity));
    }

    /// <summary>
    /// Copies all radio channels from equipped radios to the ActiveRadioComponent of an entity
    /// </summary>
    private void UpdateRadioChannels(Entity<ActiveRadioComponent, RadioVocalizerComponent> entity)
    {
        // clear all channels first
        entity.Comp1.Channels.Clear();

        // quit early if there are no ActiveRadios on the VocalizerRadioComponent
        if (entity.Comp2.ActiveRadioEntities.Count == 0)
            return;

        // loop through ActiveRadios in inventory to (re-)add channels
        foreach (var radio in entity.Comp2.ActiveRadioEntities)
        {
            // if for whatever reason this entity does not have an ActiveRadioComponent, skip it
            if (!TryComp<ActiveRadioComponent>(radio, out var activeRadioComponent))
                continue;

            // add them to the channels on the ActiveRadioComponent on the entity
            entity.Comp1.Channels.UnionWith(activeRadioComponent.Channels);
        }
    }

    /// <summary>
    /// Called whenever an entity with a VocalizerComponent tries to speak
    /// </summary>
    private void OnVocalize(Entity<RadioVocalizerComponent> ent, ref VocalizeEvent args)
    {
        if (args.Handled)
            return;

        // set to handled if we succeed in speaking on the radio
        args.Handled = TrySpeakRadio(ent.Owner, args.Message);
    }

    /// <summary>
    /// Attempts to speak on the radio. Returns false if there is no radio or talking on radio fails somehow
    /// </summary>
    /// <param name="entity">Entity to try and make speak on the radio</param>
    /// <param name="message">Message to speak</param>
    /// <returns></returns>
    private bool TrySpeakRadio(Entity<RadioVocalizerComponent?, ActiveRadioComponent?> entity, string message)
    {
        // return if this entity does not have the VocalizerRadioComponent
        if (!Resolve<RadioVocalizerComponent>(entity, ref entity.Comp1))
            return false;

        // return if this entity does not have an ActiveRadioComponent
        if (!Resolve<ActiveRadioComponent>(entity, ref entity.Comp2))
            return false;

        // return if this entity's ActiveRadioComponent contains no channels
        if (entity.Comp2.Channels.Count == 0)
            return false;

        // decide whether we actually speak using the radio
        if (!_random.Prob(entity.Comp1.RadioAttemptChance))
            return false;

        // choose random channel
        var channel = _random.Pick(entity.Comp2.Channels);
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
