using Content.Shared.RussStation.Botany.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared.RussStation.Botany.Systems;

public abstract class SharedSeedExtractorStorageSystem : EntitySystem
{
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SeedExtractorStorageComponent, GetDumpableVerbEvent>(OnGetDumpableVerb);
        SubscribeLocalEvent<SeedExtractorStorageComponent, DumpEvent>(OnDump);
    }

    private void OnGetDumpableVerb(EntityUid uid, SeedExtractorStorageComponent component, ref GetDumpableVerbEvent args)
    {
        args.Verb = Loc.GetString("seed-extractor-dump-verb", ("unit", uid));
    }

    private void OnDump(EntityUid uid, SeedExtractorStorageComponent component, ref DumpEvent args)
    {
        if (args.Handled)
            return;

        var seedContainer = Container.EnsureContainer<Container>(uid, component.SeedContainerId);
        var inserted = false;

        foreach (var entity in args.DumpQueue)
        {
            if (!_whitelist.IsWhitelistPass(component.Whitelist, entity))
                continue;

            if (Container.Insert(entity, seedContainer))
                inserted = true;
        }

        args.Handled = true;
        args.PlaySound = inserted;
    }
}
