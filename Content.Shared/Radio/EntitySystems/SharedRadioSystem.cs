using Content.Shared.Radio.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Radio.EntitySystems;

/// <summary>
///     This system handles intrinsic radios and the general process of converting radio messages into chat messages.
/// </summary>
public abstract class SharedRadioSystem : EntitySystem
{
    public void AddIntrinsicChannel(Entity<IntrinsicRadioTransmitterComponent?> ent, ProtoId<RadioChannelPrototype> channel)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.Channels.Add(channel);
        Dirty(ent);
    }

    public void RemoveIntrinsicChannel(Entity<IntrinsicRadioTransmitterComponent?> ent, ProtoId<RadioChannelPrototype> channel)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.Channels.Remove(channel);
        Dirty(ent);
    }

    public void SetIntrinsicChannels(Entity<IntrinsicRadioTransmitterComponent?> ent, HashSet<ProtoId<RadioChannelPrototype>> channels)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.Channels = channels;
        Dirty(ent);
    }
}
