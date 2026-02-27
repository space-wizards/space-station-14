using Content.Shared.CCVar;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Radiation.Events;
using Content.Shared.Rejuvenate;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Damage.Systems;

public sealed partial class DamageableSystem
{
    public override void Initialize()
    {
        RebuildContainerCache();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<DamageableComponent, ComponentInit>(DamageableInit);
        SubscribeLocalEvent<DamageableComponent, OnIrradiatedEvent>(OnIrradiated);
        SubscribeLocalEvent<DamageableComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<DamageableComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);

        _appearanceQuery = GetEntityQuery<AppearanceComponent>();
        _damageableQuery = GetEntityQuery<DamageableComponent>();

        // Damage modifier CVars are updated and stored here to be queried in other systems.
        // Note that certain modifiers requires reloading the guidebook.
        Subs.CVar(
            _config,
            CCVars.PlaytestAllDamageModifier,
            value =>
            {
                UniversalAllDamageModifier = value;
                _chemistryGuideData.ReloadAllReagentPrototypes();
                _explosion.ReloadMap();
            },
            true
        );
        Subs.CVar(
            _config,
            CCVars.PlaytestAllHealModifier,
            value =>
            {
                UniversalAllHealModifier = value;
                _chemistryGuideData.ReloadAllReagentPrototypes();
            },
            true
        );
        Subs.CVar(
            _config,
            CCVars.PlaytestProjectileDamageModifier,
            value => UniversalProjectileDamageModifier = value,
            true
        );
        Subs.CVar(
            _config,
            CCVars.PlaytestMeleeDamageModifier,
            value => UniversalMeleeDamageModifier = value,
            true
        );
        Subs.CVar(
            _config,
            CCVars.PlaytestProjectileDamageModifier,
            value => UniversalProjectileDamageModifier = value,
            true
        );
        Subs.CVar(
            _config,
            CCVars.PlaytestHitscanDamageModifier,
            value => UniversalHitscanDamageModifier = value,
            true
        );
        Subs.CVar(
            _config,
            CCVars.PlaytestReagentDamageModifier,
            value =>
            {
                UniversalReagentDamageModifier = value;
                _chemistryGuideData.ReloadAllReagentPrototypes();
            },
            true
        );
        Subs.CVar(
            _config,
            CCVars.PlaytestReagentHealModifier,
            value =>
            {
                UniversalReagentHealModifier = value;
                _chemistryGuideData.ReloadAllReagentPrototypes();
            },
            true
        );
        Subs.CVar(
            _config,
            CCVars.PlaytestExplosionDamageModifier,
            value =>
            {
                UniversalExplosionDamageModifier = value;
                _explosion.ReloadMap();
            },
            true
        );
        Subs.CVar(
            _config,
            CCVars.PlaytestThrownDamageModifier,
            value => UniversalThrownDamageModifier = value,
            true
        );
        Subs.CVar(
            _config,
            CCVars.PlaytestTopicalsHealModifier,
            value => UniversalTopicalsHealModifier = value,
            true
        );
        Subs.CVar(
            _config,
            CCVars.PlaytestMobDamageModifier,
            value => UniversalMobDamageModifier = value,
            true
        );
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (!ev.WasModified<DamageContainerPrototype>() && !ev.WasModified<DamageGroupPrototype>())
            return;

        RebuildContainerCache();
    }

    private void RebuildContainerCache()
    {
        _supportedTypesByContainer.Clear();

        foreach (var proto in _prototypeManager.EnumeratePrototypes<DamageContainerPrototype>())
        {
            var set = new HashSet<ProtoId<DamageTypePrototype>>();
            _supportedTypesByContainer[proto.ID] = set;

            foreach (var type in proto.SupportedTypes)
            {
                set.Add(type);
            }

            foreach (var groupId in proto.SupportedGroups)
            {
                var group = _prototypeManager.Index(groupId);
                foreach (var type in group.DamageTypes)
                {
                    set.Add(type);
                }
            }
        }
    }

    private void OnAfterAutoHandleState(Entity<DamageableComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        OnEntityDamageChanged(ent);
    }

    /// <summary>
    ///     Initialize a damageable component
    /// </summary>
    private void DamageableInit(Entity<DamageableComponent> ent, ref ComponentInit _)
    {
        ent.Comp.Damage.GetDamagePerGroup(_prototypeManager, ent.Comp.DamagePerGroup);
        ent.Comp.TotalDamage = ent.Comp.Damage.GetTotal();
    }

    private void OnIrradiated(Entity<DamageableComponent> ent, ref OnIrradiatedEvent args)
    {
        var damageValue = FixedPoint2.New(args.TotalRads);

        // Radiation should really just be a damage group instead of a list of types.
        DamageSpecifier damage = new();
        foreach (var typeId in ent.Comp.RadiationDamageTypeIDs)
        {
            damage.DamageDict.Add(typeId, damageValue);
        }

        ChangeDamage(ent.Owner, damage, interruptsDoAfters: false, origin: args.Origin);
    }

    private void OnRejuvenate(Entity<DamageableComponent> ent, ref RejuvenateEvent args)
    {
        // Do this so that the state changes when we set the damage
        _mobThreshold.SetAllowRevives(ent, true);
        ClearAllDamage(ent.AsNullable());
        _mobThreshold.SetAllowRevives(ent, false);
    }
}

/// <summary>
///     Raised before damage is done, so stuff can cancel it if necessary.
/// </summary>
[ByRefEvent]
public record struct BeforeDamageChangedEvent(DamageSpecifier Damage, EntityUid? Origin = null, bool Cancelled = false);

/// <summary>
///     Raised on an entity when damage is about to be dealt,
///     in case anything else needs to modify it other than the base
///     damageable component.
///
///     For example, armor.
/// </summary>
public sealed class DamageModifyEvent(DamageSpecifier damage, EntityUid? origin = null)
    : EntityEventArgs, IInventoryRelayEvent
{
    /// <inheritdoc/>
    /// <remarks>
    ///     Whenever locational damage is a thing, this should just check only that bit of armor.
    /// </remarks>
    public SlotFlags TargetSlots => ~SlotFlags.POCKET;

    /// <summary>
    ///     Contains the original damage, prior to any modifers.
    /// </summary>
    public readonly DamageSpecifier OriginalDamage = damage;

    /// <summary>
    ///     Contains the damage after modifiers have been applied.
    ///     This is the damage that will be inflicted.
    /// </summary>
    public DamageSpecifier Damage = damage;

    /// <summary>
    ///     Contains the entity which caused the damage, if any was responsible.
    /// </summary>
    public readonly EntityUid? Origin = origin;
}

public sealed class DamageChangedEvent : EntityEventArgs
{
    /// <summary>
    ///     This is the component whose damage was changed.
    /// </summary>
    /// <remarks>
    ///     Given that nearly every component that cares about a change in the damage, needs to know the
    ///     current damage values, directly passing this information prevents a lot of duplicate
    ///     Owner.TryGetComponent() calls.
    /// </remarks>
    public readonly DamageableComponent Damageable;

    /// <summary>
    ///     The amount by which the damage has changed. If the damage was set directly to some number, this will be
    ///     null.
    /// </summary>
    public readonly DamageSpecifier? DamageDelta;

    /// <summary>
    ///     Was any of the damage change dealing damage, or was it all healing?
    /// </summary>
    public readonly bool DamageIncreased;

    /// <summary>
    ///     Does this event interrupt DoAfters?
    ///     Note: As provided in the constructor, this *does not* account for DamageIncreased.
    ///     As written into the event, this *does* account for DamageIncreased.
    /// </summary>
    public readonly bool InterruptsDoAfters;

    /// <summary>
    ///     Contains the entity which caused the change in damage, if any was responsible.
    /// </summary>
    public readonly EntityUid? Origin;

    public DamageChangedEvent(
        DamageableComponent damageable,
        DamageSpecifier? damageDelta,
        bool interruptsDoAfters,
        EntityUid? origin
    )
    {
        Damageable = damageable;
        DamageDelta = damageDelta;
        Origin = origin;

        if (DamageDelta is null)
            return;

        foreach (var damageChange in DamageDelta.DamageDict.Values)
        {
            if (damageChange <= 0)
                continue;

            DamageIncreased = true;

            break;
        }

        InterruptsDoAfters = interruptsDoAfters && DamageIncreased;
    }
}
