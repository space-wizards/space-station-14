using Content.Server.Radio.Components;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Robust.Shared.Containers;

namespace Content.Server.Implants;

public sealed class RadioImplantSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadioImplantComponent, ImplantImplantedEvent>(OnImplantImplanted);
        SubscribeLocalEvent<RadioImplantComponent, EntGotRemovedFromContainerMessage>(OnRemove);
    }

    /// <summary>
    /// If implanted with a radio implant, installs the necessary intrinsic radio components
    /// </summary>
    private void OnImplantImplanted(Entity<RadioImplantComponent> ent, ref ImplantImplantedEvent args)
    {
        if (args.Implanted == null)
            return;

        var activeRadio = EnsureComp<ActiveRadioComponent>(args.Implanted.Value);
        foreach (var channel in ent.Comp.RadioChannels)
        {
            if (activeRadio.Channels.Add(channel))
                ent.Comp.ActiveAddedChannels.Add(channel);
        }

        EnsureComp<IntrinsicRadioReceiverComponent>(args.Implanted.Value);

        var intrinsicRadioTransmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(args.Implanted.Value);
        foreach (var channel in ent.Comp.RadioChannels)
        {
            if (intrinsicRadioTransmitter.Channels.Add(channel))
                ent.Comp.TransmitterAddedChannels.Add(channel);
        }
    }

    /// <summary>
    /// Removes intrinsic radio components once the Radio Implant is removed
    /// </summary>
    private void OnRemove(Entity<RadioImplantComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (TryComp<ActiveRadioComponent>(args.Container.Owner, out var activeRadioComponent))
        {
            foreach (var channel in ent.Comp.ActiveAddedChannels)
            {
                activeRadioComponent.Channels.Remove(channel);
            }
            ent.Comp.ActiveAddedChannels.Clear();

            if (activeRadioComponent.Channels.Count == 0)
            {
                RemCompDeferred<ActiveRadioComponent>(args.Container.Owner);
            }
        }

        if (!TryComp<IntrinsicRadioTransmitterComponent>(args.Container.Owner, out var radioTransmitterComponent))
            return;

        foreach (var channel in ent.Comp.TransmitterAddedChannels)
        {
            radioTransmitterComponent.Channels.Remove(channel);
        }
        ent.Comp.TransmitterAddedChannels.Clear();

        if (radioTransmitterComponent.Channels.Count == 0 || activeRadioComponent?.Channels.Count == 0)
        {
            RemCompDeferred<IntrinsicRadioTransmitterComponent>(args.Container.Owner);
        }
    }
}
