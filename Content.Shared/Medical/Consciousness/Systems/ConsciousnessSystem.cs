using Content.Shared.FixedPoint;
using Content.Shared.Medical.Consciousness.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Shared.Medical.Consciousness.Systems;

public sealed class ConsciousnessSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

    private const string UnspecifiedIdentifier = "Unspecified";

    public override void Initialize()
    {
        SubscribeLocalEvent<ConsciousnessComponent, ComponentInit>(OnConsciousnessInit);
    }

    private void OnConsciousnessInit(EntityUid uid, ConsciousnessComponent consciousness, ComponentInit args)
    {
        //set the starting consciousness to the cap if it is set to auto
        if (consciousness.RawConsciousness < 0)
        {
            consciousness.RawConsciousness = consciousness.Cap;
            Dirty(uid);
        }

        CheckConscious(uid, consciousness);
    }

    /// <summary>
    /// Add a unique consciousness modifier. This value gets added to the raw consciousness value.
    /// The owner and type combo must be unique, if you are adding multiple values from a single owner and type, combine them into one modifier
    /// </summary>
    /// <param name="target">Target entity</param>
    /// <param name="modifierOwner">Owner of a modifier</param>
    /// <param name="modifier">Value of the modifier</param>
    /// <param name="consciousness">ConsciousnessComponent</param>
    /// <param name="identifier">Localized text name for the modifier (for debug/admins)</param>
    /// <param name="type">Modifier type, defaults to generic</param>
    /// <returns>Successful</returns>
    public bool AddConsciousnessModifier(EntityUid target, EntityUid modifierOwner, FixedPoint2 modifier,
        ConsciousnessComponent? consciousness = null, string identifier = UnspecifiedIdentifier, ConsciousnessModType type = ConsciousnessModType.Generic)
    {
        if (!Resolve(target, ref consciousness) || modifier == 0)
            return false;

        if (!consciousness.Modifiers.TryAdd((modifierOwner, type), new ConsciousnessModifier(modifier, identifier)))
            return false;

        consciousness.RawConsciousness += modifier;
        var ev = new ConsciousnessUpdatedEvent(IsConscious(target, consciousness), modifier * consciousness.Multiplier);
        RaiseLocalEvent(target, ref ev, true);
        Dirty(consciousness);
        CheckConscious(target, consciousness);
        return true;
    }


    /// <summary>
    /// Get a copy of a consciousness modifier. This value gets added to the raw consciousness value.
    /// </summary>
    /// <param name="target">Target entity</param>
    /// <param name="modifierOwner">Owner of a modifier</param>
    /// <param name="modifier">copy of the found modifier, changes are NOT saved</param>
    /// <param name="consciousness">Consciousness component</param>
    /// <param name="type">Modifier type, defaults to generic</param>
    /// <returns>Successful</returns>
    public bool TryGetConsciousnessModifier(EntityUid target, EntityUid modifierOwner,
        out ConsciousnessModifier? modifier,
        ConsciousnessComponent? consciousness = null, ConsciousnessModType type = ConsciousnessModType.Generic)
    {
        modifier = null;
        if (!Resolve(target, ref consciousness) ||
            !consciousness.Modifiers.TryGetValue((modifierOwner,type), out var rawModifier))
            return false;
        modifier = rawModifier;
        return true;
    }

    /// <summary>
    /// Remove a consciousness modifier. This value gets added to the raw consciousness value.
    /// </summary>
    /// <param name="target">Target entity</param>
    /// <param name="modifierOwner">Owner of a modifier</param>
    /// <param name="consciousness">Consciousness component</param>
    /// <param name="type">Modifier type, defaults to generic</param>
    /// <returns>Successful</returns>
    public bool RemoveConsciousnessModifer(EntityUid target, EntityUid modifierOwner,
        ConsciousnessComponent? consciousness = null, ConsciousnessModType type = ConsciousnessModType.Generic)
    {
        if (!Resolve(target, ref consciousness))
            return false;
        if (!consciousness.Modifiers.Remove((modifierOwner,type), out var foundModifier))
            return false;
        consciousness.RawConsciousness = -foundModifier.Change;
        var ev = new ConsciousnessUpdatedEvent(IsConscious(target, consciousness),
            foundModifier.Change * consciousness.Multiplier);
        RaiseLocalEvent(target, ref ev, true);
        Dirty(consciousness);
        CheckConscious(target, consciousness);
        return true;
    }

    /// <summary>
    /// Edit a consciousness modifier. This value gets added to the raw consciousness value.
    /// </summary>
    /// <param name="target">Target entity</param>
    /// <param name="modifierOwner">Owner of a modifier</param>
    /// <param name="modifierChange">Value that is being added onto the modifier</param>
    /// <param name="consciousness">Consciousness component</param>
    /// <param name="type">Modifier type, defaults to generic</param>
    /// <returns>Successful</returns>
    public bool EditConsciousnessModifier(EntityUid target, EntityUid modifierOwner, FixedPoint2 modifierChange,
        ConsciousnessComponent? consciousness = null, ConsciousnessModType type = ConsciousnessModType.Generic)
    {
        if (!Resolve(target, ref consciousness) ||
            !consciousness.Modifiers.TryGetValue((modifierOwner,type), out var oldModifier))
            return false;
        var newModifier = oldModifier with {Change = oldModifier.Change + modifierChange};
        consciousness.Modifiers[(modifierOwner,type)] = newModifier;
        var ev = new ConsciousnessUpdatedEvent(IsConscious(target, consciousness),
            modifierChange * consciousness.Multiplier);
        RaiseLocalEvent(target, ref ev, true);
        Dirty(consciousness);
        CheckConscious(target, consciousness);
        return true;
    }

    /// <summary>
    /// Update the identifier string for a consciousness modifier
    /// </summary>
    /// <param name="target">Target entity</param>
    /// <param name="modifierOwner">Owner of a modifier</param>
    /// <param name="newIdentifier">New localized string to identify this modifier</param>
    /// <param name="consciousness">Consciousness component</param>
    /// <param name="type">Modifier type, defaults to generic</param>
    /// <returns>Successful</returns>
    public bool UpdateConsciousnessModifierMetaData(EntityUid target, EntityUid modifierOwner, string newIdentifier,
        ConsciousnessComponent? consciousness = null, ConsciousnessModType type = ConsciousnessModType.Generic)
    {
        if (!Resolve(target, ref consciousness) ||
            !consciousness.Modifiers.TryGetValue((modifierOwner,type), out var oldMultiplier))
            return false;
        var newMultiplier = oldMultiplier with {Identifier = newIdentifier};
        consciousness.Modifiers[(modifierOwner, type)] = newMultiplier;
        //TODO: create/raise an identifier changed event if needed
        Dirty(consciousness);
        //we don't need to check consciousness here since no simulation values get changed
        return true;
    }


    /// <summary>
    /// Add a unique consciousness multiplier. This value gets added onto the multiplier used to calculate consciousness.
    /// The owner and type combo must be unique, if you are adding multiple values from a single owner and type, combine them into one multiplier
    /// </summary>
    /// <param name="target">Target entity</param>
    /// <param name="multiplierOwner">Owner of a multiplier</param>
    /// <param name="multiplier">Value of the multiplier</param>
    /// <param name="consciousness">ConsciousnessComponent</param>
    /// <param name="identifier">Localized text name for the multiplier (for debug/admins)</param>
    /// <param name="type">Multiplier type, defaults to generic</param>
    /// <returns>Successful</returns>
    public bool AddConsciousnessMultiplier(EntityUid target, EntityUid multiplierOwner, FixedPoint2 multiplier,
        ConsciousnessComponent? consciousness = null, string identifier = UnspecifiedIdentifier, ConsciousnessModType type = ConsciousnessModType.Generic)
    {
        if (!Resolve(target, ref consciousness) || multiplier == 0)
            return false;

        if (!consciousness.Multipliers.TryAdd((multiplierOwner,type), new ConsciousnessMultiplier(multiplier, identifier)))
            return false;
        var oldMultiplier = consciousness.Multiplier;
        consciousness.Multiplier += multiplier;
        var ev = new ConsciousnessUpdatedEvent(IsConscious(target, consciousness),
            multiplier * consciousness.RawConsciousness);
        RaiseLocalEvent(target, ref ev, true);
        Dirty(consciousness);
        CheckConscious(target, consciousness);
        return true;
    }

    /// <summary>
    /// Get a copy of a consciousness multiplier. This value gets added onto the multiplier used to calculate consciousness.
    /// </summary>
    /// <param name="target">Target entity</param>
    /// <param name="multiplierOwner">Owner of a multiplier</param>
    /// <param name="multiplier">Copy of the found multiplier, changes are NOT saved</param>
    /// <param name="consciousness">Consciousness component</param>
    /// <param name="type">Multiplier type, defaults to generic</param>
    /// <returns>Successful</returns>
    public bool TryGetConsciousnessMultiplier(EntityUid target, EntityUid multiplierOwner,
        out ConsciousnessMultiplier? multiplier, ConsciousnessComponent? consciousness = null,
        ConsciousnessModType type = ConsciousnessModType.Generic)
    {
        multiplier = null;
        if (!Resolve(target, ref consciousness) ||
            !consciousness.Multipliers.TryGetValue((multiplierOwner, type), out var rawMultiplier))
            return false;
        multiplier = rawMultiplier;
        return true;
    }

    /// <summary>
    /// Remove a consciousness multiplier. This value gets added onto the multiplier used to calculate consciousness.
    /// </summary>
    /// <param name="target">Target entity</param>
    /// <param name="multiplierOwner">Owner of a multiplier</param>
    /// <param name="type">Multiplier type, defaults to generic</param>
    /// <param name="consciousness">Consciousness component</param>
    /// <returns>Successful</returns>
    public bool RemoveConsciousnessMultiplier(EntityUid target, EntityUid multiplierOwner,
        ConsciousnessModType type = ConsciousnessModType.Generic,
        ConsciousnessComponent? consciousness = null)
    {
        if (!Resolve(target, ref consciousness))
            return false;
        if (!consciousness.Multipliers.Remove((multiplierOwner, type), out var foundMultiplier))
            return false;
        consciousness.Multiplier = -foundMultiplier.Change;
        var ev = new ConsciousnessUpdatedEvent(IsConscious(target, consciousness),
            foundMultiplier.Change * consciousness.RawConsciousness);
        RaiseLocalEvent(target, ref ev, true);
        Dirty(consciousness);
        CheckConscious(target, consciousness);
        return true;
    }

    /// <summary>
    /// Edit a consciousness multiplier. This value gets added onto the multiplier used to calculate consciousness.
    /// </summary>
    /// <param name="target">Target entity</param>
    /// <param name="multiplierOwner">Owner of a multiplier</param>
    /// <param name="multiplierChange">Value that is being added onto the multiplier</param>
    /// <param name="type">Multiplier type, defaults to generic</param>
    /// <param name="consciousness">Consciousness component</param>
    /// <returns>Successful</returns>
    public bool EditConsciousnessMultiplier(EntityUid target, EntityUid multiplierOwner, FixedPoint2 multiplierChange,
        ConsciousnessComponent? consciousness = null, ConsciousnessModType type = ConsciousnessModType.Generic)
    {
        if (!Resolve(target, ref consciousness) ||
            !consciousness.Multipliers.TryGetValue((multiplierOwner, type), out var oldMultiplier))
            return false;
        var newMultiplier = oldMultiplier with {Change = oldMultiplier.Change + multiplierChange};
        consciousness.Multipliers[(multiplierOwner, type)] = newMultiplier;
        var ev = new ConsciousnessUpdatedEvent(IsConscious(target, consciousness),
            multiplierChange * consciousness.RawConsciousness);
        RaiseLocalEvent(target, ref ev, true);
        Dirty(consciousness);
        CheckConscious(target, consciousness);
        return true;
    }

    /// <summary>
    /// Update the identifier string for a consciousness multiplier
    /// </summary>
    /// <param name="target">Target entity</param>
    /// <param name="multiplierOwner">Owner of a multiplier</param>
    /// <param name="newIdentifier">New localized string to identify this multiplier</param>
    /// <param name="type">Multiplier type, defaults to generic</param>
    /// <param name="consciousness">Consciousness component</param>
    /// <returns>Sucessful</returns>
    public bool UpdateConsciousnessMultiplierMetaData(EntityUid target, EntityUid multiplierOwner, string newIdentifier,
        ConsciousnessComponent? consciousness = null, ConsciousnessModType type = ConsciousnessModType.Generic)
    {
        if (!Resolve(target, ref consciousness) ||
            !consciousness.Multipliers.TryGetValue((multiplierOwner, type), out var oldMultiplier))
            return false;
        var newMultiplier = oldMultiplier with {Identifier = newIdentifier};
        consciousness.Multipliers[(multiplierOwner, type)] = newMultiplier;
        //TODO: create/raise an identifier changed event if needed
        Dirty(consciousness);
        //we don't need to check consciousness here since no simulation values get changed
        return true;
    }

    /// <summary>
    /// Checks to see if an entity should be made unconscious, this is called whenever any consciousness values are changed.
    /// Unless you are directly modifying a consciousness component (pls dont) you don't need to call this.
    /// </summary>
    /// <param name="target">Target entity</param>
    /// <param name="consciousness">Consciousness component</param>
    public void CheckConscious(EntityUid target, ConsciousnessComponent? consciousness = null)
    {
        if (!Resolve(target, ref consciousness))
            return;
        if (consciousness.Consciousness > consciousness.Threshold)
        {
            if (consciousness.IsConscious)
                return;
            SetConscious(target, true, consciousness);
            Dirty(target);
        }
        else
        {
            if (!consciousness.IsConscious)
                return;
            SetConscious(target, false, consciousness);
            Dirty(target);
        }
    }

    /// <summary>
    /// Gets the current consciousness state of an entity. This is mainly used internally.
    /// </summary>
    /// <param name="target">Target entity</param>
    /// <param name="consciousness">Consciousness component</param>
    /// <returns>True if conscious</returns>
    public bool IsConscious(EntityUid target, ConsciousnessComponent? consciousness = null)
    {
        return Resolve(target, ref consciousness) && consciousness.Consciousness > consciousness.Threshold;
    }

    /// <summary>
    /// Get all consciousness multipliers present on an entity. Note: these are copies, do not try to edit the values
    /// </summary>
    /// <param name="target">target entity</param>
    /// <param name="consciousness">consciousness component</param>
    /// <returns>Enumerable of Modifiers</returns>
    public IEnumerable<((EntityUid,ConsciousnessModType), ConsciousnessModifier)> GetAllModifiers(EntityUid target,
        ConsciousnessComponent? consciousness = null)
    {
        if (!Resolve(target, ref consciousness))
            yield break;
        foreach (var (owner, modifier) in consciousness.Modifiers)
        {
            yield return (owner, modifier);
        }
    }

    /// <summary>
    /// Get all consciousness multipliers present on an entity. Note: these are copies, do not try to edit the values
    /// </summary>
    /// <param name="target">target entity</param>
    /// <param name="consciousness">consciousness component</param>
    /// <returns>Enumerable of Multipliers</returns>
    public IEnumerable<((EntityUid,ConsciousnessModType), ConsciousnessMultiplier)> GetAllMultipliers(EntityUid target,
        ConsciousnessComponent? consciousness = null)
    {
        if (!Resolve(target, ref consciousness))
            yield break;
        foreach (var (owner, multiplier) in consciousness.Multipliers)
        {
            yield return (owner, multiplier);
        }
    }

    /// <summary>
    /// Only used internally. Do not use this, instead use consciousness modifiers/multipliers!
    /// </summary>
    /// <param name="target">target entity</param>
    /// <param name="isConscious">should this entity be conscious</param>
    /// <param name="consciousness">consciousness component</param>
    /// <param name="mobState">mobState component</param>
    private void SetConscious(EntityUid target, bool isConscious, ConsciousnessComponent? consciousness = null,
        MobStateComponent? mobState = null)
    {
        if (!Resolve(target, ref mobState, ref consciousness) || consciousness.IsConscious == isConscious)
            return;
        _mobStateSystem.ChangeMobState(target, isConscious ? MobState.Alive : MobState.Critical, mobState);
        consciousness.IsConscious = isConscious;
        Dirty(target);
    }
}
