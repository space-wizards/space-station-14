using Content.Shared.FixedPoint;
using Content.Shared.Medical.Consciousness.Components;
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
        }
    }

    public bool AddConsciousnessModifier(EntityUid target, EntityUid modifierOwner, FixedPoint2 modifier,
        ConsciousnessComponent? consciousness = null, string identifier = UnspecifiedIdentifier)
    {
        if (!Resolve(target, ref consciousness))
            return false;

        if (!consciousness.Modifiers.TryAdd(modifierOwner, new ConsciousnessModifier(modifier, identifier)))
            return false;

        consciousness.RawConsciousness += modifier;
        Dirty(consciousness);
        return true;
    }

    public bool TryGetConsciousnessModifier(EntityUid target, EntityUid modifierOwner,
        out ConsciousnessModifier? modifier,
        ConsciousnessComponent? consciousness = null)
    {
        modifier = null;
        if (!Resolve(target, ref consciousness) ||
            !consciousness.Modifiers.TryGetValue(modifierOwner, out var rawModifier))
            return false;
        modifier = rawModifier;
        return true;
    }

    public bool AddConsciousnessMultiplier(EntityUid target, EntityUid multiplierOwner, FixedPoint2 multiplier,
        ConsciousnessComponent? consciousness = null, string identifier = UnspecifiedIdentifier)
    {
        if (!Resolve(target, ref consciousness))
            return false;

        if (!consciousness.Multipliers.TryAdd(multiplierOwner, new ConsciousnessMultiplier(multiplier, identifier)))
            return false;

        consciousness.Multiplier += multiplier;
        Dirty(consciousness);
        return true;
    }

    public bool TryGetConsciousnessMultiplier(EntityUid target, EntityUid multiplierOwner,
        out ConsciousnessMultiplier? multiplier,
        ConsciousnessComponent? consciousness = null)
    {
        multiplier = null;
        if (!Resolve(target, ref consciousness) ||
            !consciousness.Multipliers.TryGetValue(multiplierOwner, out var rawMultiplier))
            return false;
        multiplier = rawMultiplier;
        return true;
    }

}
