using Content.Shared.Emag.Systems;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws.Components;

namespace Content.Shared.Silicons.Laws;

public abstract partial class SharedSiliconLawSystem
{
    public void InitializeProvider()
    {
        SubscribeLocalEvent<SiliconLawProviderComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SiliconLawProviderComponent, IonStormLawsEvent>(OnIonStormLaws);
        SubscribeLocalEvent<SiliconLawProviderComponent, ComponentShutdown>(OnProviderShutdown);

        SubscribeLocalEvent<SiliconLawProviderComponent, GetSiliconLawsEvent>(OnProviderGetLaws);
    }

    #region Events
    private void OnMapInit(Entity<SiliconLawProviderComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.Lawset = GetLawset(ent.Comp.Laws);

        // In case we ourselves are lawbound, link to this ourselves.
        // Also caches our laws.
        LinkToProvider(ent.Owner, ent.AsNullable());
    }

    private void OnProviderShutdown(Entity<SiliconLawProviderComponent> ent, ref ComponentShutdown args)
    {
        var iterateEntities = ent.Comp.ExternalLawsets;
        foreach (var lawbound in iterateEntities)
        {
            UnlinkFromProvider(lawbound, ent.AsNullable());
        }
    }

    private void OnIonStormLaws(Entity<SiliconLawProviderComponent> ent, ref IonStormLawsEvent args)
    {
        // Emagged borgs are immune to ion storm
        if (!_emag.CheckFlag(ent, EmagType.Interaction))
        {
            var newLaws = args.Lawset.Clone();

            // Show the silicon has been subverted.
            ent.Comp.Subverted = true;

            SetProviderLaws(ent.AsNullable(), newLaws.Laws);

            // new laws may allow antagonist behaviour so make it clear for admins
            if(_mind.TryGetMind(ent.Owner, out var mindId, out _))
                EnsureSubvertedSiliconRole(mindId);
        }
    }

    private void OnProviderGetLaws(Entity<SiliconLawProviderComponent> ent, ref GetSiliconLawsEvent args)
    {
        // The chassis is handled seperately in its own event.
        if (HasComp<BorgChassisComponent>(ent))
            return;

        args.Laws = ent.Comp.Lawset.Clone();
        args.Handled = true;
    }
    #endregion Events
}
