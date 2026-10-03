using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.Radio.EntitySystems;

/// <summary>
/// This system handles intrinsic radios and the general process of converting radio messages into chat messages.
/// </summary>
public abstract partial class SharedRadioSystem : EntitySystem
{
    /// <summary>
    /// Adds a single <see cref="RadioChannelPrototype"/> to an entity's <see cref="IntrinsicRadioTransmitterComponent"/>.
    /// </summary>
    /// <param name="ent">The entity to add the channel for.</param>
    /// <param name="channel">The channel to add.</param>
    /// <returns>True if added successfully, otherwise False.</returns>
    public bool AddIntrinsicTransmitterChannel(Entity<Components.IntrinsicRadioTransmitterComponent?> ent, ProtoId<RadioChannelPrototype> channel)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        var success = ent.Comp.Channels.Add(channel);
        Dirty(ent);

        return success;
    }

    /// <summary>
    /// Removes <see cref="RadioChannelPrototype"/> from an entity's <see cref="IntrinsicRadioTransmitterComponent"/>.
    /// </summary>
    /// <param name="ent">The entity to remove the channel from.</param>
    /// <param name="channel">The channel to remove.</param>
    /// <returns>True if removed successfully, otherwise False.</returns>
    public bool RemoveIntrinsicTransmitterChannel(Entity<Components.IntrinsicRadioTransmitterComponent?> ent, ProtoId<RadioChannelPrototype> channel)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        var success = ent.Comp.Channels.Remove(channel);
        Dirty(ent);

        return success;
    }

    /// <summary>
    /// Sets the channel list in a <see cref="IntrinsicRadioTransmitterComponent"/> to an existing Hashset.
    /// Used when you don't wanna have to add or remove every channel one by one.
    /// </summary>
    /// <param name="ent">The entity to set the channels for.</param>
    /// <param name="channels">Hashset of the channels to set.</param>
    /// <returns>True if set successfully, otherwise False.</returns>
    public bool SetIntrinsicTransmitterChannels(Entity<Components.IntrinsicRadioTransmitterComponent?> ent, HashSet<ProtoId<RadioChannelPrototype>> channels)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        ent.Comp.Channels = channels;
        Dirty(ent);

        return true;
    }

    /// <summary>
    /// Send radio message to all active radio listeners.
    /// </summary>
    [PublicAPI]
    public void SendRadioMessage(EntityUid messageSource,
        string message,
        ProtoId<RadioChannelPrototype> channel,
        EntityUid radioSource,
        bool escapeMarkup = true)
    {
        SendRadioMessage(messageSource, message, ProtoMan.Index(channel), radioSource, escapeMarkup: escapeMarkup);
    }

    /// <summary>
    /// Sends a radio message to all active radio listeners.
    /// </summary>
    /// <param name="messageSource">Entity that spoke the message.</param>
    /// <param name="message">Message to send over the radio.</param>
    /// <param name="channel">Radio channel to send the message on.</param>
    /// <param name="radioSource">Entity transmitting the message.</param>
    /// <param name="escapeMarkup">Whether markup in the message should be escaped.</param>
    [PublicAPI]
    public virtual void SendRadioMessage(EntityUid messageSource,
        string message,
        RadioChannelPrototype channel,
        EntityUid radioSource,
        bool escapeMarkup = true)
    {

    }
}
