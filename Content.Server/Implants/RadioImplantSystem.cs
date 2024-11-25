using Robust.Shared.Containers;
using Content.Shared.FixedPoint;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Tag;
using Content.Server.Radio.Components;

namespace Content.Server.Implants;

public sealed class RadioImplantSystem : EntitySystem
{
    [ValidatePrototypeId<TagPrototype>]
    public const string RadioImplantTag = "RadioImplant";
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadioImplantComponent, ImplantImplantedEvent>(OnImplantImplantedEvent);
        SubscribeLocalEvent<RadioImplantComponent, EntGotRemovedFromContainerMessage>(OnRemove);
    }

    /// <summary>
    /// If implanted with a radio implant, installs the necessary intrinsic radio components
    /// </summary>
    private void OnImplantImplantedEvent(EntityUid uid, RadioImplantComponent comp, ref ImplantImplantedEvent ev)
    {
        if (ev.Implanted == null)
            return;

        var activeRadio = EnsureComp<ActiveRadioComponent>(ev.Implanted.Value);

        foreach (var channel in comp.RadioChannels)
        {
            if (activeRadio.Channels.Add(channel))
                comp.AddedChannels.Add(channel);
        }

        EnsureComp<IntrinsicRadioReceiverComponent>(ev.Implanted.Value);

        var intrinsicRadioTransmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(ev.Implanted.Value);
        foreach (var channel in comp.RadioChannels)
        {
            if (intrinsicRadioTransmitter.Channels.Add(channel))
                comp.AddedChannels.Add(channel);
        }
    }

    /// <summary>
    /// Removes intrinsic radio components once the Radio Implant is removed
    /// </summary>
    private void OnRemove(EntityUid uid, RadioImplantComponent comp, EntGotRemovedFromContainerMessage args)
    {
        if (TryComp<ActiveRadioComponent>(args.Container.Owner, out var activeRadioComponent))
        {
            foreach (var channel in comp.AddedChannels)
            {
                activeRadioComponent.Channels.Remove(channel);
            }
            if (activeRadioComponent.Channels.Count == FixedPoint2.Zero)
            {
                RemCompDeferred<ActiveRadioComponent>(args.Container.Owner);
            }
        }

        if (!TryComp<IntrinsicRadioTransmitterComponent>(args.Container.Owner, out var radioTransmitterComponent))
            return;

        foreach (var channel in comp.AddedChannels)
        {
            radioTransmitterComponent.Channels.Remove(channel);
        }
        if (radioTransmitterComponent.Channels.Count == FixedPoint2.Zero || activeRadioComponent is { Channels.Count: 0 })
        {
            RemCompDeferred<IntrinsicRadioTransmitterComponent>(args.Container.Owner);
        }
    }
}
