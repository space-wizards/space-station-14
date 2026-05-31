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
        SubscribeLocalEvent<PermanentStatusEffectsComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PermanentStatusEffectsComponent, ComponentRemove>(OnRemove);
    }

    private void OnMapInit(Entity<PermanentStatusEffectsComponent> ent, ref MapInitEvent args)
    {
        EnsureStatusEffects(ent);
    }

    private void OnStartup(Entity<PermanentStatusEffectsComponent> ent, ref ComponentStartup args)
    {
        // MapInit is preferred because the entity and its containers are fully initialized by then.
        // This startup path only exists for components added after map initialization.
        if (LifeStage(ent) < EntityLifeStage.MapInitialized)
            return;

        EnsureStatusEffects(ent);
    }

    private void OnRemove(Entity<PermanentStatusEffectsComponent> ent, ref ComponentRemove args)
    {
        foreach (var effect in ent.Comp.StatusEffects)
        {
            _statusEffects.TryRemoveStatusEffect(ent, effect);
        }
    }

    private void EnsureStatusEffects(Entity<PermanentStatusEffectsComponent> ent)
    {
        foreach (var effect in ent.Comp.StatusEffects)
        {
            _statusEffects.TrySetStatusEffectDuration(ent, effect);
        }
    }
}
