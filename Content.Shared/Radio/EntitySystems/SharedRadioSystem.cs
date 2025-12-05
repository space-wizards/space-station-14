using Content.Shared.Radio.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Radio.EntitySystems;

/// <summary>
///     This system handles intrinsic radios and the general process of converting radio messages into chat messages.
/// </summary>
public abstract class SharedRadioSystem : EntitySystem
{
    public bool AddIntrinsicTransmitterChannel(Entity<IntrinsicRadioTransmitterComponent?> ent, ProtoId<RadioChannelPrototype> channel)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        var success = ent.Comp.Channels.Add(channel);
        Dirty(ent);

        return success;
    }

    public bool RemoveIntrinsicTransmitterChannel(Entity<IntrinsicRadioTransmitterComponent?> ent, ProtoId<RadioChannelPrototype> channel)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        var success = ent.Comp.Channels.Remove(channel);
        Dirty(ent);

        return success;
    }

    public bool SetIntrinsicTransmitterChannels(Entity<IntrinsicRadioTransmitterComponent?> ent, HashSet<ProtoId<RadioChannelPrototype>> channels)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        ent.Comp.Channels = channels;
        Dirty(ent);

        return true;
    }
}
