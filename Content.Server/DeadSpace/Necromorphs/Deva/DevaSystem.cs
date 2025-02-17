// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Content.Server.DeadSpace.Abilities.Cocoon;
using Content.Server.DeadSpace.Abilities.Cocoon.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Server.Inventory;
using Robust.Shared.Timing;
using System.Linq;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Damage;
using Content.Server.Chat.Systems;
using Content.Shared.Speech.Components;
using Robust.Shared.Audio.Systems;
using Content.Shared.DeadSpace.Necromorphs.Deva;
using Robust.Shared.Physics.Components;

namespace Content.Server.DeadSpace.Necromorphs.Deva;

public sealed class DevaSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly CocoonSystem _cocoon = default!;
    [Dependency] private readonly ServerInventorySystem _inventory = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DevaComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<DevaComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<DevaComponent, DevaEnrageActionEvent>(DoEnrage);
        SubscribeLocalEvent<DevaComponent, MobStateChangedEvent>(OnState);
        SubscribeLocalEvent<DevaComponent, RefreshMovementSpeedModifiersEvent>(OnRefresh);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var devki = EntityQueryEnumerator<DevaComponent>();
        while (devki.MoveNext(out var uid, out var devaComp))
        {
            if (devaComp.IsEnrageState)
            {
                LookupSacrifice(uid, devaComp);
            }
            if (devaComp.IsTrappedVictim)
            {
                VictimDamage(uid, devaComp);
            }
        }
    }
    private void OnComponentInit(EntityUid uid, DevaComponent component, ComponentInit args)
    {
        EnsureComp<CocoonComponent>(uid);
        _actions.AddAction(uid, ref component.DevaEnrageActionEntity, component.DevaEnrageAction, uid);
        component.NextTickUtilEnrage = _gameTiming.CurTime;
        component.NextTickUtilPrison = _gameTiming.CurTime;
        component.DamageTick = _gameTiming.CurTime;
    }
    private void OnShutdown(EntityUid uid, DevaComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.DevaEnrageActionEntity);
    }
    private void OnState(EntityUid uid, DevaComponent component, MobStateChangedEvent args)
    {
        if (_mobState.IsDead(uid))
            _cocoon.TryEmptyCocoon(uid);
    }
    private void OnRefresh(EntityUid uid, DevaComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.MovementSpeedMultiplier, component.MovementSpeedMultiplier);
    }
    private void DoEnrage(EntityUid uid, DevaComponent component, DevaEnrageActionEvent args)
    {
        if (args.Handled)
            return;

        component.MovementSpeedMultiplier = component.MovementSpeedEnrage;
        _movement.RefreshMovementSpeedModifiers(uid);
        component.IsEnrageState = true;
        component.NextTickUtilEnrage = _gameTiming.CurTime + TimeSpan.FromSeconds(component.DurationEnrage);

        if (component.EnrageSound != null)
            _audio.PlayPvs(component.EnrageSound, uid);

        args.Handled = true;
    }
    public void UpdateDeva(EntityUid uid, DevaComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!component.IsTrappedVictim)
        {
            _inventory.TryUnequip(uid, "outerClothing", true, true);
            var item = Spawn("ClothingDevaOpen", Transform(uid).Coordinates);
            _inventory.TryEquip(uid, item, "outerClothing", true, true);
        }
        else
        {
            _inventory.TryUnequip(uid, "outerClothing", true, true);
            var item = Spawn("ClothingDevaClose", Transform(uid).Coordinates);
            _inventory.TryEquip(uid, item, "outerClothing", true, true);
        }
    }
    private void LookupSacrifice(EntityUid uid, DevaComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (_gameTiming.CurTime >= component.NextTickUtilEnrage)
        {
            OffEnrage(uid, component);
            return;
        }

        if (!TryComp<PhysicsComponent>(uid, out var physics))
            return;

        var ents = _lookup.GetEntitiesInRange<MobStateComponent>(_transform.GetMapCoordinates(uid, Transform(uid)), component.Range).ToList();
        var validEntities = ents
            .Where(ent => !_npcFaction.IsEntityFriendly(uid, ent.Owner) && ent.Owner != uid && _mobState.IsAlive(ent.Owner)
            && TryComp<PhysicsComponent>(ent.Owner, out var physicsEnt) && physics.Mass * 2 > physicsEnt.Mass)
            .ToList();

        if (validEntities.Count > 0)
        {
            var ent = validEntities.FirstOrDefault();
            Sharpen(uid, ent, component);
        }
    }
    private void Sharpen(EntityUid uid, EntityUid target, DevaComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!_cocoon.TryInsertCocoon(uid, target))
            return;

        component.IsTrappedVictim = true;
        component.NextTickUtilPrison = _gameTiming.CurTime + TimeSpan.FromSeconds(component.DurationPrison);
        OffEnrage(uid, component);
        UpdateDeva(uid, component);
    }
    private void VictimDamage(EntityUid uid, DevaComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (_gameTiming.CurTime <= component.DamageTick)
            return;

        if (_gameTiming.CurTime >= component.NextTickUtilPrison)
        {
            ReleasePrisoner(uid, component);
            return;
        }

        EntityUid? prisonerTarget = _cocoon.GetPrisoner(uid);

        if (!TryComp<DamageableComponent>(prisonerTarget, out var damageable))
            return;

        _damageable.TryChangeDamage(prisonerTarget, component.Damage, false, false, damageable);

        if (TryComp<VocalComponent>(prisonerTarget, out var vocal))
        {
            var random = new Random();
            int chance = random.Next(0, 5);

            if (chance < 1)
            {
                _chat.TryPlayEmoteSound(prisonerTarget.Value, vocal.EmoteSounds, "Crying");
            }
            else
            {
                _chat.TryPlayEmoteSound(prisonerTarget.Value, vocal.EmoteSounds, "Scream");
            }
        }

        if (component.EatSound != null)
            _audio.PlayPvs(component.EatSound, uid);

        component.DamageTick = _gameTiming.CurTime + TimeSpan.FromSeconds(1f);
    }
    private void OffEnrage(EntityUid uid, DevaComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.MovementSpeedMultiplier = component.MovementSpeed;
        _movement.RefreshMovementSpeedModifiers(uid);
        component.IsEnrageState = false;
        component.NextTickUtilEnrage = _gameTiming.CurTime;
    }
    private void ReleasePrisoner(EntityUid uid, DevaComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _cocoon.TryEmptyCocoon(uid);
        component.IsTrappedVictim = false;
        component.NextTickUtilPrison = _gameTiming.CurTime;
        UpdateDeva(uid, component);
    }
}
