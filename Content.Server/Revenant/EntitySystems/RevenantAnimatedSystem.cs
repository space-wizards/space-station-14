using Content.Shared.Popups;
using Content.Shared.Damage;
using Content.Server.Revenant.Components;
using Content.Shared.DoAfter;
using Content.Shared.Revenant.Components;
using Content.Server.NPC.HTN;
using Content.Server.NPC;
using Content.Shared.Weapons.Melee;
using Content.Shared.CombatMode;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Robust.Shared.Prototypes;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Movement.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Cuffs.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Player;
using Content.Shared.Explosion.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs;
using Robust.Shared.Timing;

namespace Content.Server.Revenant.EntitySystems;

public sealed partial class RevenantAnimatedSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly NpcFactionSystem _factionSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ItemToggleSystem _itemToggleSystem = default!;
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _moveSpeed = default!;
    [Dependency] private readonly MobThresholdSystem _thresholds = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevenantAnimatedComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<RevenantAnimatedComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<RevenantAnimatedComponent, MobStateChangedEvent>(OnMobStateChange);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<RevenantAnimatedComponent>();

        while (enumerator.MoveNext(out var uid, out var animate))
        {
            if (animate.EndTime == null)
                continue;

            if (animate.EndTime <= _gameTiming.CurTime)
                InanimateTarget(uid, animate);
        }
    }

    private void OnComponentStartup(Entity<RevenantAnimatedComponent> ent, ref ComponentStartup _)
    {
        if (ent.Comp.LifeStage != ComponentLifeStage.Starting)
            return;

        // Turn on welding rods and stun prods
        if (HasComp<ItemToggleMeleeWeaponComponent>(ent.Owner) && TryComp<ItemToggleComponent>(ent.Owner, out var toggle))
            _itemToggleSystem.TryActivate((ent.Owner, toggle));

        _popup.PopupEntity(Loc.GetString("revenant-animate-item-animate", ("name", Comp<MetaDataComponent>(ent.Owner).EntityName)), ent.Owner, Filter.Pvs(ent.Owner), true);

        // Add melee damage if an item doesn't already have it
        if (EnsureHelper<MeleeWeaponComponent>(ent, out var melee))
            melee.Damage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Blunt"), 5);

        EnsureHelper<InputMoverComponent>(ent);
        EnsureHelper<MovementSpeedModifierComponent>(ent, out var moveSpeed);
        if (ent.Comp.Revenant != null)
            _moveSpeed.ChangeBaseSpeed(ent,
                ent.Comp.Revenant.Value.Comp.AnimateWalkSpeed,
                ent.Comp.Revenant.Value.Comp.AnimateSprintSpeed,
                MovementSpeedModifierComponent.DefaultAcceleration,
                moveSpeed
            );
        else
            _moveSpeed.ChangeBaseSpeed(ent,
                RevenantComponent.DefaultAnimateWalkSpeed,
                RevenantComponent.DefaultAnimateSprintSpeed,
                MovementSpeedModifierComponent.DefaultAcceleration,
                moveSpeed
            );

        EnsureHelper<NpcFactionMemberComponent>(ent, out var factions);
        _factionSystem.ClearFactions((ent, factions));
        _factionSystem.AddFaction((ent, factions), "SimpleHostile");

        // For things like handcuffs
        EnsureHelper<DoAfterComponent>(ent);

        EnsureHelper<HTNComponent>(ent, out var htn);
        if (HasComp<GunComponent>(ent))
        {
            // Goals: Magdump into any nearby creatures, and melee hit them if empty
            if (TryComp<ChamberMagazineAmmoProviderComponent>(ent, out var bolt))
                _gunSystem.SetBoltClosed(ent, bolt, true);
            htn.RootTask = new HTNCompoundTask() { Task = "SimpleRangedHostileCompound" };
        }
        else if (HasComp<HandcuffComponent>(ent))
            // Goals: Jump into any creature's pockets/hands and cuff them
            htn.RootTask = new HTNCompoundTask() { Task = "AnimatedHandcuffsCompound" };
        else if (HasComp<OnUseTimerTriggerComponent>(ent))
            // Goals: Jump into any creature's pockets/hands and activate self
            htn.RootTask = new HTNCompoundTask() { Task = "AnimatedGrenadeCompound" };
        else
            // Goals: Fist fight anyone near you
            htn.RootTask = new HTNCompoundTask() { Task = "SimpleHostileCompound" };

        htn.Blackboard.SetValue(NPCBlackboard.Owner, ent.Owner);

        EnsureHelper<DamageableComponent>(ent);
        EnsureHelper<MobStateComponent>(ent);
        EnsureHelper<MobThresholdsComponent>(ent, out var thresholds);
        _thresholds.SetMobStateThreshold(ent, 30, MobState.Dead, thresholds);

        EnsureHelper<CombatModeComponent>(ent);
    }

    private void OnComponentShutdown(Entity<RevenantAnimatedComponent> ent, ref ComponentShutdown _)
    {
        if (ent.Comp.LifeStage != ComponentLifeStage.Stopping)
            return;

        foreach (var comp in ent.Comp.AddedComponents)
        {
            if (comp.Deleted)
                continue;

            RemCompDeferred(ent, comp);
        }

        _popup.PopupEntity(Loc.GetString("revenant-animate-item-inanimate", ("name", Comp<MetaDataComponent>(ent).EntityName)), ent, Filter.Pvs(ent), true);
    }

    private void OnMobStateChange(Entity<RevenantAnimatedComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            InanimateTarget(ent);
        }
    }

    // Returns true if a new component was added to the target.
    private bool EnsureHelper<T>(Entity<RevenantAnimatedComponent> target, out T comp) where T : Component, new()
    {
        if (TryComp<T>(target, out var existing))
        {
            comp = existing;
            return false;
        }

        comp = AddComp<T>(target);
        target.Comp.AddedComponents.Add(comp);
        return true;
    }

    private bool EnsureHelper<T>(Entity<RevenantAnimatedComponent> target) where T : Component, new()
    {
        return EnsureHelper<T>(target, out _);
    }

    public bool CanAnimateObject(EntityUid target)
    {
        return !(HasComp<MindContainerComponent>(target) || HasComp<HTNComponent>(target) || HasComp<RevenantAnimatedComponent>(target));
    }

    public bool TryAnimateObject(EntityUid target, TimeSpan? duration = null, Entity<RevenantComponent>? revenant = null)
    {
        if (!CanAnimateObject(target))
            return false;

        var animate = EnsureComp<RevenantAnimatedComponent>(target);
        animate.Revenant = revenant;
        if (duration != null)
            animate.EndTime = _gameTiming.CurTime + duration.Value;
        else if (revenant != null)
            animate.EndTime = _gameTiming.CurTime + revenant.Value.Comp.AnimateTime;

        return true;
    }

    public void InanimateTarget(EntityUid target, RevenantAnimatedComponent? comp = null)
    {
        if (!target.Valid || !Resolve(target, ref comp))
            return;

        RemComp<RevenantAnimatedComponent>(target);
    }
}