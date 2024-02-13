using Content.Server.Emp;
using Content.Server.Radio.Components;
using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Radio.EntitySystems;
using Robust.Shared.Player;

namespace Content.Server.Radio.EntitySystems;

/// <summary>
/// Manages the transmission of radio messages to listeners.
/// </summary>
public sealed class RadioSystem : SharedHeadsetSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HeadsetComponent, EncryptionChannelsChangedEvent>(OnKeysChanged);
        SubscribeLocalEvent<HeadsetComponent, EmpPulseEvent>(OnEmpPulse);

        SubscribeLocalEvent<HeadsetComponent, EntityRadioedEvent>(OnHeadsetReceive);
        SubscribeLocalEvent<IntrinsicRadioComponent, EntityRadioedEvent>(OnIntrinsicRadioReceive);
    }

    private static void OnEmpPulse(EntityUid uid, HeadsetComponent component, ref EmpPulseEvent args)
    {
        if (!component.Enabled)
            return;

        args.Affected = true;
        args.Disabled = true;
    }

    private void OnHeadsetReceive(EntityUid uid, HeadsetComponent headset, EntityRadioedEvent ev)
    {
        if (!TryComp<ActorComponent>(headset.CurrentlyWornBy, out var actor))
            return;

        RaiseNetworkEvent(ev, actor.PlayerSession);
    }

    private void OnIntrinsicRadioReceive(EntityUid uid, IntrinsicRadioComponent _, ref EntityRadioedEvent ev)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        RaiseNetworkEvent(ev, actor.PlayerSession);
    }

    private void OnKeysChanged(EntityUid uid, HeadsetComponent component, EncryptionChannelsChangedEvent args)
    {
        UpdateHeadsetRadioChannels(uid, component, args.Component);
    }

    protected override void OnGotEquipped(EntityUid uid, HeadsetComponent component, GotEquippedEvent args)
    {
        base.OnGotEquipped(uid, component, args);

        if (component.CurrentlyWornBy == null || !component.Enabled)
            return;

        UpdateHeadsetRadioChannels(uid, component);
        UpdateUserRadioChannels(uid, component.CurrentlyWornBy.Value, component);
    }

    protected override void OnGotUnequipped(EntityUid uid, HeadsetComponent component, GotUnequippedEvent args)
    {
        base.OnGotUnequipped(uid, component, args);

        RemComp<RadioableComponent>(uid);

        RemoveChannelsFromUser(args.Equipee);
    }

    public void SetEnabled(EntityUid uid, bool isEnabled, HeadsetComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Enabled == isEnabled)
            return;

        if (!isEnabled)
        {
            RemCompDeferred<RadioableComponent>(uid);

            if (component.CurrentlyWornBy != null)
                RemoveChannelsFromUser(component.CurrentlyWornBy.Value);

            return;
        }

        if (component.CurrentlyWornBy == null)
            return;

        UpdateHeadsetRadioChannels(uid, component);
        UpdateUserRadioChannels(uid, component.CurrentlyWornBy.Value, component);
    }

    private void UpdateHeadsetRadioChannels(EntityUid uid, HeadsetComponent headset, EncryptionKeyHolderComponent? keyHolder = null)
    {
        // make sure to not add Radioable when headset is being deleted
        if (!headset.Enabled || MetaData(uid).EntityLifeStage >= EntityLifeStage.Terminating)
            return;

        if (!Resolve(uid, ref keyHolder))
            return;

        if (keyHolder.Channels.Count == 0)
            RemComp<RadioableComponent>(uid);
        else
            EnsureComp<RadioableComponent>(uid).Channels = new HashSet<string>(keyHolder.Channels);
    }

    /// <summary>
    /// Make sure the user of the headset can radio on the channels provided by the headset, without messing up their
    /// capacity to innately radio (e.g. via an implant, being a borg...)
    /// </summary>
    private void UpdateUserRadioChannels(EntityUid equipment, EntityUid equipee, HeadsetComponent headset)
    {
        // make sure to not add Radioable when headset is being deleted
        if (!headset.Enabled || MetaData(equipment).EntityLifeStage >= EntityLifeStage.Terminating)
            return;

        if (!TryComp<RadioableComponent>(equipment, out var radio))
            return;

        var channels = new HashSet<string>(radio.Channels);

        if (TryComp<IntrinsicRadioTransmitterComponent>(equipee, out var intrinsicRadio))
        {
            foreach (var intrinsicChannel in intrinsicRadio.Channels)
            {
                channels.Add(intrinsicChannel);
            }
        }

        if (channels.Count == 0)
            RemComp<RadioableComponent>(equipee);
        else
            EnsureComp<RadioableComponent>(equipee).Channels = channels;
    }

    /// <summary>
    /// Make sure that the user loses access to any radio channels they only have via the headset.
    /// </summary>
    private void RemoveChannelsFromUser(EntityUid equipee)
    {
        var channels = new HashSet<string>();

        if (TryComp<IntrinsicRadioTransmitterComponent>(equipee, out var intrinsicRadio))
        {
            foreach (var intrinsicChannel in intrinsicRadio.Channels)
            {
                channels.Add(intrinsicChannel);
            }
        }

        if (channels.Count > 0)
            EnsureComp<RadioableComponent>(equipee).Channels = channels;
        else
            RemComp<RadioableComponent>(equipee);
    }
}
