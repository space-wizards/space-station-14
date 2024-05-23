using Content.Shared.Medical.Digestion.Components;

namespace Content.Shared.Medical.Digestion.Systems;

public sealed class DigestionSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<DigestionComponent, ComponentInit>(OnCompInit);
    }

    private void OnCompInit(Entity<DigestionComponent> ent, ref ComponentInit args)
    {
        
    }
}
