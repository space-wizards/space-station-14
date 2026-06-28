using Content.Shared.StatusEffectNew.Components;

namespace Content.Shared.StatusEffectNew;

/// <summary>
/// Keeps permanent status effects from <see cref="PermanentStatusEffectsComponent"/> applied
/// for as long as the owning component exists.
/// </summary>
public sealed partial class PermanentStatusEffectsSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PermanentStatusEffectsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PermanentStatusEffectsComponent, ComponentRemove>(OnRemove);
    }

    private void OnMapInit(Entity<PermanentStatusEffectsComponent> ent, ref MapInitEvent args)
    {
        foreach (var effect in ent.Comp.StatusEffects)
        {
            _statusEffects.TrySetStatusEffectDuration(ent, effect);
        }
    }

    private void OnRemove(Entity<PermanentStatusEffectsComponent> ent, ref ComponentRemove args)
    {
        foreach (var effect in ent.Comp.StatusEffects)
        {
            _statusEffects.TryRemoveStatusEffect(ent, effect);
        }
    }
}
