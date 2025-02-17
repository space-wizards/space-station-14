// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.Body.Systems;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead;
using Content.Server.Chat.Systems;
using Content.Server.Emoting.Systems;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Systems;

namespace Content.Server.DeadSpace.Necromorphs.InfectionDead;

public sealed partial class NecromorfSystem : SharedInfectionDeadSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NecromorfComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<NecromorfComponent, EmoteEvent>(OnEmote, before:
            new[] { typeof(VocalSystem), typeof(BodyEmotesSystem) });

        SubscribeLocalEvent<NecromorfComponent, TryingToSleepEvent>(OnSleepAttempt);
        SubscribeLocalEvent<NecromorfComponent, GetCharactedDeadIcEvent>(OnGetCharacterDeadIC);
        SubscribeLocalEvent<NecromorfComponent, IsEquippingAttemptEvent>(OnEquipAttempt);
        SubscribeLocalEvent<NecromorfComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }

    private void OnRefreshSpeed(EntityUid uid, NecromorfComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.MovementSpeedMultiply, component.MovementSpeedMultiply);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;

        // Heal
        var necroQuery = EntityQueryEnumerator<NecromorfComponent, DamageableComponent, MobStateComponent>();
        while (necroQuery.MoveNext(out var uid, out var comp, out var damage, out var mobState))
        {
            // Process only once per second
            if (comp.NextTick + TimeSpan.FromSeconds(1) > curTime)
                continue;

            comp.NextTick = curTime;

            if (_mobState.IsDead(uid, mobState))
                continue;

            var multiplier = _mobState.IsCritical(uid, mobState)
                ? comp.PassiveHealingCritMultiplier
                : 1f;

            // Gradual healing for living Necromorfs.
            _damageable.TryChangeDamage(uid, comp.PassiveHealing * multiplier, true, false, damage);
        }
    }

    private void OnSleepAttempt(EntityUid uid, NecromorfComponent component, ref TryingToSleepEvent args)
    {
        args.Cancelled = true;
    }

    private void OnGetCharacterDeadIC(EntityUid uid, NecromorfComponent component, ref GetCharactedDeadIcEvent args)
    {
        args.Dead = true;
    }

    private void OnEquipAttempt(EntityUid uid, NecromorfComponent component, IsEquippingAttemptEvent args)
    {
        if (!component.IsCanUseInventory)
        {
            args.Cancel();
            return;
        }
    }

    private void OnStartup(EntityUid uid, NecromorfComponent component, ComponentStartup args)
    {
        if (component.EmoteSoundsId == null)
            return;

        _protoManager.TryIndex(component.EmoteSoundsId, out component.EmoteSounds);
    }

    private void OnEmote(EntityUid uid, NecromorfComponent component, ref EmoteEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = _chat.TryPlayEmoteSound(uid, component.EmoteSounds, args.Emote);
    }
}
