using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;

namespace Content.Server.StatusEffectNew;

/// <inheritdoc/>
public sealed partial class StatusEffectsSystem : SharedStatusEffectsSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusEffectContainerComponent, ComponentShutdown>(OnContainerShutdown);
    }

    private void OnContainerShutdown(Entity<StatusEffectContainerComponent> ent, ref ComponentShutdown args)
    {
        foreach (var effect in ent.Comp.ActiveStatusEffects)
        {
            QueueDel(effect);
        }
    }
}
