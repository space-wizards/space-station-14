using Content.Shared.Starlight.Antags.Abductor;
using Content.Shared.Interaction;
using Content.Shared.Starlight.Medical.Surgery;
using Content.Shared.DoAfter;
using Content.Shared.Body.Systems;
using Content.Shared.Starlight.Medical.Surgery.Steps.Parts;
using Content.Shared.Body.Organ;

namespace Content.Server.Starlight.Antags.Abductor;

public sealed partial class AbductorSystem : SharedAbductorSystem
{
    
    [Dependency] private readonly SharedBodySystem _body = default!;
    
    public void InitializeExtractor()
    {
        SubscribeLocalEvent<AbductorExtractorComponent, AfterInteractEvent>(OnExtractorInteract);
        
        SubscribeLocalEvent<AbductorExtractorComponent, AbductorExtractDoAfterEvent>(OnExtractDoAfter);
    }
    
    private void OnExtractorInteract(Entity<AbductorExtractorComponent> ent, ref AfterInteractEvent args)
    {
        if (!_actionBlockerSystem.CanInstrumentInteract(args.User, args.Used, args.Target) 
            || !args.Target.HasValue 
            || !_body.TryGetBodyOrganEntityComps<OrganHeartComponent>(args.Target.Value, out var hearts) 
            || hearts.Count < 1)
            return;

        if (HasComp<SurgeryTargetComponent>(args.Target))
            Extract(ent, args.Target.Value, args.User);
    }

    public void Extract(Entity<AbductorExtractorComponent> ent, EntityUid target, EntityUid user)
    {
        var time = TimeSpan.FromSeconds(2);

        var doAfter = new DoAfterArgs(EntityManager, user, time, new AbductorExtractDoAfterEvent(), ent, target, ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            DistanceThreshold = 1f
        };
        _doAfter.TryStartDoAfter(doAfter);
    }
    
    private void OnExtractDoAfter(Entity<AbductorExtractorComponent> ent, ref AbductorExtractDoAfterEvent args)
    {
        if (args.Target is null) return;
        
        if (!_body.TryGetBodyOrganEntityComps<OrganHeartComponent>(args.Target.Value, out var hearts))
            return;
        
        foreach (var heart in hearts)
            _body.RemoveOrgan(heart, _entityManager.GetComponent<OrganComponent>(heart));
    }
}