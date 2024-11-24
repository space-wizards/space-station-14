using Content.Server.Radio.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server.Implants;

public sealed class RadioImplantSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IServerEntityManager _entityManager = default!;


    [ValidatePrototypeId<TagPrototype>]
    public const string RadioImplantTag = "RadioImplant";
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadioImplantComponent, ImplantImplantedEvent>(RadioImplantCheck);
        SubscribeLocalEvent<RadioImplantComponent, EntGotRemovedFromContainerMessage>(OnRemove);
    }

    /// <summary>
    /// Checks if the implant was a Radio Implant or not
    /// </summary>
    public void RadioImplantCheck(EntityUid uid, RadioImplantComponent comp, ref ImplantImplantedEvent ev)
    {
        if (!_tag.HasTag(ev.Implant, RadioImplantTag) || ev.Implanted == null)
            return;

        var activeRadio = EnsureComp<ActiveRadioComponent>(ev.Implanted.Value);
        activeRadio.Channels.Add(comp.RadioChannel);

        var intrinsicRadioReceiver = EnsureComp<IntrinsicRadioReceiverComponent>(ev.Implanted.Value);
        var intrinsicRadioTransmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(ev.Implanted.Value);
        intrinsicRadioTransmitter.Channels.Add(comp.RadioChannel);
    }

    /// <summary>
    /// Removes intrinsic radio components once the Radio Implant is removed
    /// </summary>
    public void OnRemove(EntityUid uid, RadioImplantComponent component, EntGotRemovedFromContainerMessage args)
    {
        if (TryComp<ActiveRadioComponent>(args.Container.Owner, out var activeRadioComponent))
        {
            activeRadioComponent.Channels.Remove(component.RadioChannel);
            if (activeRadioComponent.Channels.Count == FixedPoint2.Zero)
            {
                _entityManager.RemoveComponent(args.Container.Owner, activeRadioComponent);
            }
        }

        if (!TryComp<IntrinsicRadioTransmitterComponent>(args.Container.Owner, out var radioTransmitterComponent))
            return;

        radioTransmitterComponent.Channels.Remove(component.RadioChannel);
        if (radioTransmitterComponent.Channels.Count == FixedPoint2.Zero || activeRadioComponent is { Channels.Count: 0 })
        {
            _entityManager.RemoveComponent(args.Container.Owner, radioTransmitterComponent);
        }
    }
}
