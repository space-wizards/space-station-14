using Content.Server.Animals.Components;
using Content.Server.Chat.Systems;
using Content.Server.Radio;
using Content.Server.Radio.Components;
using Content.Shared.Chat;
using Content.Shared.Clothing;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Animals.Systems;

/// <summary>
/// ParrotRadioSystem handles parroting things via equipped radios if a ParrotSpeakEvent from a ParrotSystem is fired
/// Also relays messages received on the radio to the ParrotSystem for learning
/// </summary>
public sealed partial class ParrotRadioSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ParrotSystem _parrot = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParrotRadioComponent, ClothingDidEquippedEvent>(OnClothingEquipped);
        SubscribeLocalEvent<ParrotRadioComponent, ClothingDidUnequippedEvent>(OnClothingUnequipped);

        SubscribeLocalEvent<ParrotRadioComponent, MapInitEvent>(RadioOnMapInit);

        SubscribeLocalEvent<ParrotRadioComponent, RadioReceiveEvent>(OnRadioReceive);

        SubscribeLocalEvent<ParrotSpeakerComponent, ParrotSpeakEvent>(OnParrotSpeak);
    }

    /// <summary>
    /// Callback for when this entity equips clothing or has clothing equipped by something
    /// Used to update the ActiveRadio entities a parrot with a ParrotRadioComponent uses to listen to radios and
    /// talk on radio channels
    /// </summary>
    private void OnClothingEquipped(Entity<ParrotRadioComponent> entity, ref ClothingDidEquippedEvent args)
    {
        // only care if the equipped clothing item has an ActiveRadioComponent
        if (!HasComp<ActiveRadioComponent>(args.Clothing))
            return;

        entity.Comp.ActiveRadioEntities.Add(args.Clothing.Owner);

        // update active radio channels
        UpdateParrotRadioChannels(entity.Owner);
    }

    /// <summary>
    /// Called if this entity unequipped clothing or has clothing unequipped by something
    /// Used to update the ActiveRadio entities a parrot with a ParrotRadioComponent uses to listen to radios and
    /// talk on radio channels
    /// </summary>
    private void OnClothingUnequipped(Entity<ParrotRadioComponent> entity, ref ClothingDidUnequippedEvent args)
    {
        // try to remove this item from the active radio entities list
        // if this returns false, the item wasn't found so it was never a radio we cared about, quit early
        if (!entity.Comp.ActiveRadioEntities.Remove(args.Clothing.Owner))
            return;

        // update active radio channels
        UpdateParrotRadioChannels(entity.Owner);
    }

    /// <summary>
    /// Called when the map is initialized (or after an entity is created).
    /// This ensures radio channels are updated when an entity spawns in with a radio pre-equipped
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="args"></param>
    private void RadioOnMapInit(Entity<ParrotRadioComponent> entity, ref MapInitEvent args)
    {
        // If an entity has a ParrotRadioComponent it really ought to have an ActiveRadioComponent
        var activeRadio = EnsureComp<ActiveRadioComponent>(entity);

        UpdateParrotRadioChannels((entity, activeRadio));
    }

    private void OnRadioReceive(Entity<ParrotRadioComponent> entity, ref RadioReceiveEvent args)
    {
        _parrot.TryLearn(entity.Owner, args.Message, args.MessageSource);
    }

    /// <summary>
    /// Copies all radio channels from equipped radios to the ActiveRadioComponent of an entity
    /// </summary>
    private void UpdateParrotRadioChannels(Entity<ActiveRadioComponent?, ParrotRadioComponent?> entity)
    {
        // return if the expected components are not on this entity
        if (!Resolve<ActiveRadioComponent>(entity, ref entity.Comp1))
            return;

        if (!Resolve<ParrotRadioComponent>(entity, ref entity.Comp2))
            return;

        // clear all channels first
        entity.Comp1.Channels.Clear();

        // quit early if there are no ActiveRadios on the ParrotRadioComponent
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
    /// Called whenever an entity with a ParrotSpeakComponent tries to speak
    /// </summary>
    private void OnParrotSpeak(Entity<ParrotSpeakerComponent> ent, ref ParrotSpeakEvent args)
    {
        // set to handled if we succeed in speaking on the radio
        args.Handled = TrySpeakRadio(ent.Owner, args.Message);
    }

    /// <summary>
    /// Attempts to speak on the radio. Returns false if there is no radio or talking on radio fails somehow
    /// </summary>
    /// <param name="entity">Entity to try and make speak on the radio</param>
    /// <param name="message">Message to speak </param>
    /// <returns></returns>
    private bool TrySpeakRadio(Entity<ParrotRadioComponent?, ActiveRadioComponent?> entity, string message)
    {
        // return if this entity does not have the ParrotRadioComponent
        if (!Resolve<ParrotRadioComponent>(entity, ref entity.Comp1))
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

        _chat.TrySendInGameICMessage(
            entity,
            $"{SharedChatSystem.RadioChannelPrefix}{channelPrefix} {message}",
            InGameICChatType.Whisper,
            ChatTransmitRange.Normal);

        return true;
    }
}
