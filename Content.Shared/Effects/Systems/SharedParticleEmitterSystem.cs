using Content.Shared.Effects.Components;

namespace Content.Shared.Effects.Systems;

public abstract partial class SharedParticleEmitterSystem : EntitySystem
{
    public void SetEnabled(Entity<ParticleEmitterComponent?> ent, bool enabled)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (enabled)
            EnsureComp<ActiveParticleEmitterComponent>(ent.Owner);
        else
            RemComp<ActiveParticleEmitterComponent>(ent.Owner);
    }
}
