using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Radio.Components;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;

namespace Content.Shared.Radio.EntitySystems;

public abstract class SharedHeadsetSystem : EntitySystem
{
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HeadsetComponent, InventoryRelayedEvent<GetDefaultRadioChannelEvent>>(OnGetDefault);
        SubscribeLocalEvent<HeadsetComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<HeadsetComponent, GotUnequippedEvent>(OnGotUnequipped);

        SubscribeLocalEvent<HeadsetComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnGetDefault(EntityUid uid, HeadsetComponent component, InventoryRelayedEvent<GetDefaultRadioChannelEvent> args)
    {
        if (!component.Enabled || !component.IsEquipped)
        {
            // don't provide default channels from pocket slots.
            return;
        }

        if (TryComp(uid, out EncryptionKeyHolderComponent? keyHolder))
            args.Args.Channel ??= keyHolder.DefaultChannel; 
    }

    protected virtual void OnGotEquipped(EntityUid uid, HeadsetComponent component, GotEquippedEvent args)
    {
        component.IsEquipped = args.SlotFlags.HasFlag(component.RequiredSlot);
    }

    protected virtual void OnGotUnequipped(EntityUid uid, HeadsetComponent component, GotUnequippedEvent args)
    {
        component.IsEquipped = false;
    }

    private void OnGetVerbs(EntityUid uid, HeadsetComponent component, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract || !args.CanComplexInteract || !args.CanAccess)
            return;

        if (!TryComp<EncryptionKeyHolderComponent>(uid, out var keyHolder))
            return;

        var priority = 0;

        foreach (var channel in keyHolder.Channels)
        {
            var name = _prototype.Index<RadioChannelPrototype>(channel).LocalizedName;

            var toggled = component.ToggledSoundChannels.Contains(channel);

            args.Verbs.Add(new()
            {
                Text = toggled ? $"[bold]{name}" : name,
                Priority = priority++,
                Category = VerbCategory.ToggleHeadsetSound,
                Act = () => ToggleHeadsetSound((uid, component), channel, !toggled)
            });
        }
    }

    /// <summary>
    /// Toggles channel on given headset to on or off.
    /// </summary>
    /// <param name="on">Whether to toggle this channel as emitting sound.</param>
    public static void ToggleHeadsetSound(Entity<HeadsetComponent> headset, string channel, bool on)
    {
        if (on)
            headset.Comp.ToggledSoundChannels.Add(channel);
        else
            headset.Comp.ToggledSoundChannels.Remove(channel);
    }
}
