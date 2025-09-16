using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Radio.Components;

namespace Content.Server.Implants;

public sealed class RadioImplantSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadioImplantComponent, ImplantImplantedEvent>(OnImplantImplanted);
        SubscribeLocalEvent<RadioImplantComponent, ImplantRemovedEvent>(OnImplantRemoved);
    }

    /// <summary>
    /// If implanted with a radio implant, installs the necessary intrinsic radio components
    /// </summary>
    private void OnImplantImplanted(Entity<RadioImplantComponent> ent, ref ImplantImplantedEvent args)
    {
        var activeRadio = EnsureComp<ActiveRadioComponent>(args.Implanted);
        foreach (var channel in ent.Comp.RadioChannels)
        {
            if (activeRadio.Channels.Add(channel))
                ent.Comp.ActiveAddedChannels.Add(channel);
        }

        EnsureComp<IntrinsicRadioReceiverComponent>(args.Implanted);

        var intrinsicRadioTransmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(args.Implanted);
        foreach (var channel in ent.Comp.RadioChannels)
        {
            if (intrinsicRadioTransmitter.Channels.Add(channel))
                ent.Comp.TransmitterAddedChannels.Add(channel);
        }
    }

    /// <summary>
    /// Removes intrinsic radio components once the Radio Implant is removed
    /// </summary>
    private void OnImplantRemoved(Entity<RadioImplantComponent> ent, ref ImplantRemovedEvent args)
    {
        if (TryComp<ActiveRadioComponent>(args.Implanted, out var activeRadioComponent))
        {
            foreach (var channel in ent.Comp.ActiveAddedChannels)
            {
                activeRadioComponent.Channels.Remove(channel);
            }
            ent.Comp.ActiveAddedChannels.Clear();

            if (activeRadioComponent.Channels.Count == 0)
            {
                RemCompDeferred<ActiveRadioComponent>(args.Implanted);
            }
        }

        if (!TryComp<IntrinsicRadioTransmitterComponent>(args.Implanted, out var radioTransmitterComponent))
            return;

        foreach (var channel in ent.Comp.TransmitterAddedChannels)
        {
            radioTransmitterComponent.Channels.Remove(channel);
        }
        ent.Comp.TransmitterAddedChannels.Clear();

        if (radioTransmitterComponent.Channels.Count == 0 || activeRadioComponent?.Channels.Count == 0)
        {
            RemCompDeferred<IntrinsicRadioTransmitterComponent>(args.Implanted);
        }
    }
}
