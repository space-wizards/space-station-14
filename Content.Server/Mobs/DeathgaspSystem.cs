using Content.Server.Chat.Systems;
using Content.Server.Speech.EntitySystems;
using Content.Server.Speech.Muting;
using Content.Shared.Chat;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Speech.Muting;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Mobs;

/// <see cref="DeathgaspComponent"/>
public sealed class DeathgaspSystem: EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    // DS14-start
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    // DS14-end

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeathgaspComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<DeathgaspComponent, EmoteEvent>(OnEmote, before: [typeof(VocalSystem)]); // DS14
    }

    private void OnMobStateChanged(EntityUid uid, DeathgaspComponent component, MobStateChangedEvent args)
    {
        // don't deathgasp if they arent going straight from crit to dead
        if (args.NewMobState != MobState.Dead || args.OldMobState is not (MobState.Critical or MobState.PreCritical)) // DS14 edited
            return;

        Deathgasp(uid, component);
    }

    // DS14-start
    private void OnEmote(EntityUid uid, DeathgaspComponent component, ref EmoteEvent args)
    {
        if (args.Handled ||
            args.Emote.ID != component.Prototype ||
            !_inventory.TryGetSlotEntity(uid, "mask", out var maskUid) ||
            !TryComp<SpecialDeathSoundComponent>(maskUid, out var special))
        {
            return;
        }

        _audio.PlayPvs(special.Sound, uid);
        args.Handled = true;
    }
    // DS14-end

    /// <summary>
    ///     Causes an entity to perform their deathgasp emote, if they have one.
    /// </summary>
    public bool Deathgasp(EntityUid uid, DeathgaspComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        if (HasComp<MutedComponent>(uid))
            return false;

        _chat.TryEmoteWithChat(uid, component.Prototype, ignoreActionBlocker: true);

        return true;
    }
}
