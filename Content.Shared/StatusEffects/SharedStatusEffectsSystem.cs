using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffects.Components;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.StatusEffects;

/// <summary>
/// The skeleton of status effects.
/// </summary>
public abstract partial class SharedStatusEffectsSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly MobThresholdSystem _thresholdSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusEffectsComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StatusEffectComponent, ComponentStartup>(OnEffectStartup);

        // Event relays down here
        SubscribeLocalEvent<StatusEffectsComponent, MeleeHitEvent>(RelayEvent);
        SubscribeLocalEvent<StatusEffectsComponent, BeforeDamageChangedEvent>(RefRelayEvent);
        SubscribeLocalEvent<StatusEffectsComponent, DamageModifyEvent>(RelayEvent);
        SubscribeLocalEvent<StatusEffectsComponent, DamageChangedEvent>(RelayEvent);
        SubscribeLocalEvent<StatusEffectsComponent, GetStatusIconsEvent>(RefRelayEvent);

        // InitializeActivation();
        // InitializeEffects();
    }

    #region Events

    /// <summary>
    /// The entire stat effect economy will collapse without this.
    /// </summary>
    private void OnStartup(EntityUid uid, StatusEffectsComponent component, ComponentStartup args)
    {
        component.StatusContainer = _container.EnsureContainer<Container>(uid, component.StatusContainerId);
        component.StatusContainer.OccludesLight = false;
    }

    private void OnEffectStartup(EntityUid uid, StatusEffectComponent comp, ComponentStartup args)
    {
        comp.EndTime = Timing.CurTime + TimeSpan.FromSeconds(comp.DefaultLength);
    }

    // TODO: Might be good to just rewrite some of this.

    #endregion

    #region Funcitons

    // /// <summary>
    // /// Used to apply a status effect onto an entity "The Intended Way™"
    // /// </summary>
    // /// <param name="uid">The player that is recieving the status effect</param>
    // /// <param name="effectProto">The prototype of the status effect being applied</param>
    // /// <param name="stacks">How powerful is the effect?</param>
    // /// <param name="newLength">How long should the effect last</param>
    // /// <param name="addOn">Should this add stacks to the entity or simply just override it?</param>
    // /// <param name="overrideEffect">If true, and there is already an effect present, it will be overwritten</param>
    // /// <param name="comp"></param>
    // public EntityUid? ApplyEffect(
    //     EntityUid uid,
    //     string effectProtoStr,
    //     int stacks = 1,
    //     TimeSpan? newLength = null,
    //     bool addOn = true,
    //     bool overrideEffect = false,
    //     StatusEffectsComponent? comp = null)
    // {
    //     if (!Resolve(uid, ref comp) || comp.StatusContainer == null || stacks <= 0)
    //         return null;

    //     if (!PrototypeManager.TryIndex<EntityPrototype>(effectProtoStr, out var effectPrototype))
    //     {
    //         Log.Error($"Entity prototype of '{effectProtoStr}' could not be found.");
    //         return null;
    //     }

    //     foreach (var storedEffect in comp.StatusContainer.ContainedEntities)
    //     {
    //         if (TryComp<StatusEffectComponent>(storedEffect, out var effectComp) &&
    //             TryComp<MetaDataComponent>(storedEffect, out var metaData) && metaData.EntityPrototype == effectPrototype)
    //         {
    //             if (addOn)
    //                 ModifyEffect(storedEffect, effectComp.Stacks + stacks, newLength, overrideEffect, effectComp);
    //             else
    //                 ModifyEffect(storedEffect, stacks, newLength, overrideEffect, effectComp);

    //             return storedEffect;
    //         }
    //     }

    //     var effect = Spawn(effectProtoStr, Transform(uid).Coordinates);
    //     ModifyEffect(effect, stacks, newLength, true);

    //     comp.StatusContainer.Insert(effect);

    //     return effect;
    // }

    public void ModifyEffect(
        EntityUid uid,
        int newStacks,
        TimeSpan? newLength = null,
        EffectModifyMode effectApplyType = EffectModifyMode.Override,
        StatusEffectComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        var curTime = Timing.CurTime;
        newStacks = ModifyStacks(newStacks, comp.MaxStacks);

        switch (effectApplyType)
        {
            case EffectModifyMode.Override:
                comp.Stacks = newStacks;
                if (newLength != null)
                    comp.EndTime = curTime + newLength.Value;
                break;
            case EffectModifyMode.UseHighestStack:
                if (newStacks < comp.Stacks)
                    break;
                comp.Stacks = newStacks;

                if (newLength != null && curTime + newLength.Value <= comp.EndTime)
                    comp.EndTime = curTime + newLength.Value;
                break;
            case EffectModifyMode.AddTime:
                if (newLength == null)
                    break;

                if (newStacks > comp.Stacks)
                {
                    comp.EndTime = curTime + newLength.Value + (comp.EndTime - curTime) * (comp.Stacks / newStacks);
                    comp.Stacks = newStacks;
                }
                else
                    comp.EndTime = curTime + (comp.EndTime - curTime) + newLength.Value * (newStacks / comp.Stacks);
                break;
            case EffectModifyMode.AddStacks:
                comp.Stacks = ModifyStacks(comp.Stacks + newStacks, comp.MaxStacks);
                break;
        }
    }

    private static int ModifyStacks(int stacks, int maxStacks)
    {
        if (maxStacks > 0)
            return Math.Clamp(stacks, 0, maxStacks);
        else
            return stacks;
    }

    #endregion

    #region Relays

    /// <summary>
    /// Used to relay an event that an entity recieved into it's effects so that the event can be modified by the effects.
    /// </summary>
    private void RelayEvent<TEvent>(EntityUid uid, StatusEffectsComponent comp, TEvent args)
    {
        var relayedArgs = new StatusEffectRelayEvent<TEvent>(args, uid);

        if (comp.StatusContainer == null)
            return;

        foreach (var effect in comp.StatusContainer.ContainedEntities)
        {
            RaiseLocalEvent(effect, relayedArgs);
        }
    }

    /// <summary>
    /// A ref version of RelayEvent, which is used to relay an event that an entity recieved into it's effects.
    /// </summary>
    private void RefRelayEvent<TEvent>(EntityUid uid, StatusEffectsComponent comp, ref TEvent args)
    {
        var relayedArgs = new StatusEffectRelayEvent<TEvent>(args, uid);

        if (comp.StatusContainer == null)
            return;

        foreach (var effect in comp.StatusContainer.ContainedEntities)
        {
            RaiseLocalEvent(effect, relayedArgs);
        }
    }

    #endregion
}


public enum EffectModifyMode
{
    Override, // Should completely override the effect.
    UseHighestStack, // Uses the highest stack effect
    AddTime, // Adds the time of the two effects together, considering their stack sizes. It will also override
    AddStacks, // Adds the stacks together. Doesn't modify the time.
}
