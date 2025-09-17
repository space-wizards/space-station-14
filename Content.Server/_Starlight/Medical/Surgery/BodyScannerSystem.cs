using Content.Shared.DeviceLinking.Events;
using Content.Shared.Buckle.Components;
using Content.Shared.Starlight.Medical.Surgery.Effects.Step;
using Content.Shared.Starlight.Medical.Surgery;
using Content.Server.Medical;
using Content.Server.Medical.Components;
using System.Linq;

namespace Content.Server.Starlight.Medical.Surgery;

public sealed partial class BodyScannerSystem : SharedBodyScannerSystem
{
    [Dependency] private readonly HealthAnalyzerSystem _healthAnalyzer = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<OperatingTableComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<OperatingTableComponent, UnstrappedEvent>(OnUnstrapped);
        
        SubscribeLocalEvent<BodyScannerComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<BodyScannerComponent, PortDisconnectedEvent>(OnPortDisconnected);
    }
    
    private void OnStrapped(Entity<OperatingTableComponent> ent, ref StrappedEvent args)
    {        
        if (ent.Comp.Scanner != null && TryComp<HealthAnalyzerComponent>(ent.Comp.Scanner, out var analyzer))
            _healthAnalyzer.BeginAnalyzingEntity((ent.Comp.Scanner.Value, analyzer), args.Buckle.Owner);
    }
    
    private void OnUnstrapped(Entity<OperatingTableComponent> ent, ref UnstrappedEvent args)
    {
        if (ent.Comp.Scanner != null && TryComp<HealthAnalyzerComponent>(ent.Comp.Scanner, out var analyzer))
            _healthAnalyzer.StopAnalyzingEntity((ent.Comp.Scanner.Value, analyzer), args.Buckle.Owner);
    }
    
    private void OnNewLink(Entity<BodyScannerComponent> ent, ref NewLinkEvent args)
    {
        if (!TryComp<OperatingTableComponent>(args.Sink, out var table) || !TryComp<StrapComponent>(args.Sink, out var strap))
            return;

        ent.Comp.TableEntity = args.Sink;
        
        // Why one? Because operating table don't have more than one slot, and also Health Analyzer works only with one target.
        if (TryComp<HealthAnalyzerComponent>(ent.Owner, out var analyzer) && strap.BuckledEntities.Count == 1)
            _healthAnalyzer.StopAnalyzingEntity((ent.Owner, analyzer), strap.BuckledEntities.First());
        
        table.Scanner = ent.Owner;
        Dirty(args.Sink, table);
        Dirty(ent);
    }
    
    private void OnPortDisconnected(Entity<BodyScannerComponent> ent, ref PortDisconnectedEvent args)
    {
        var tableEntityUid = ent.Comp.TableEntity;
        if (args.Port != ent.Comp.LinkingPort || tableEntityUid == null)
            return;

        if (TryComp<OperatingTableComponent>(tableEntityUid, out var table))
        {
            table.Scanner = null;
            Dirty(tableEntityUid.Value, table);
        }
        
        if (TryComp<HealthAnalyzerComponent>(ent.Owner, out var analyzer) && analyzer.ScannedEntity != null)
            _healthAnalyzer.StopAnalyzingEntity((ent.Owner, analyzer), analyzer.ScannedEntity.Value);
        
        ent.Comp.TableEntity = null;
        Dirty(ent);
    }
}