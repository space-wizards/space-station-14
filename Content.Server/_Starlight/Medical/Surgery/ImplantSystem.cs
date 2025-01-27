using Content.Shared.Starlight.Medical.Surgery.Events;
using Content.Shared.Starlight.Medical.Surgery.Steps.Parts;
using Robust.Shared.GameObjects;

namespace Content.Server.Starlight.Medical.Surgery;
// Based on the RMC14.
// https://github.com/RMC-14/RMC-14
public sealed partial class ImplantSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<OrganImplantComponent, SurgeryOrganInsertCompleted>(OnOrganInsertComplete);
        
        SubscribeLocalEvent<OrganImplantComponent, SurgeryOrganExtractCompleted>(OnOrganExtractComplete);
    }
    
    private void OnOrganInsertComplete(Entity<OrganImplantComponent> ent, ref SurgeryOrganInsertCompleted args)
    {
        foreach (var comp in (ent.Comp.AddComp ?? []).Values)
        {
            if (!EntityManager.HasComponent(args.Body, comp.Component.GetType()))
                EntityManager.AddComponent(args.Body, _compFactory.GetComponent(comp.Component.GetType()));
        }
    }
    
    private void OnOrganExtractComplete(Entity<OrganImplantComponent> ent, ref SurgeryOrganExtractCompleted args)
    {
        foreach (var comp in (ent.Comp.AddComp ?? []).Values)
        {
            if (EntityManager.HasComponent(args.Body, comp.Component.GetType()))
                EntityManager.RemoveComponent(args.Body, _compFactory.GetComponent(comp.Component.GetType()));
        }
    }
}