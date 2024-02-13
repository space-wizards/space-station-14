using Content.Shared.Clothing;
using Content.Shared.CriminalRecords.Components;
using Content.Shared.CriminalRecords.Systems;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Inventory;
using Content.Shared.Security.Components;
using Robust.Shared.Containers;

namespace Content.Shared.IdentityManagement;

public abstract class SharedIdentitySystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly SharedCriminalRecordsConsoleSystem _criminalRecordsConsole = default!;
    private static string SlotName = "identity";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IdentityComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<IdentityComponent, IdentityChangedEvent>(OnIdentityChanged);
        SubscribeLocalEvent<IdentityBlockerComponent, SeeIdentityAttemptEvent>(OnSeeIdentity);
        SubscribeLocalEvent<IdentityBlockerComponent, InventoryRelayedEvent<SeeIdentityAttemptEvent>>((e, c, ev) => OnSeeIdentity(e, c, ev.Args));
        SubscribeLocalEvent<IdentityBlockerComponent, ItemMaskToggledEvent>(OnMaskToggled);
    }

    private void OnSeeIdentity(EntityUid uid, IdentityBlockerComponent component, SeeIdentityAttemptEvent args)
    {
        if (component.Enabled)
        {
            args.TotalCoverage |= component.Coverage;
            if(args.TotalCoverage == IdentityBlockerCoverage.FULL)
                args.Cancel();
        }
    }

    protected virtual void OnComponentInit(EntityUid uid, IdentityComponent component, ComponentInit args)
    {
        component.IdentityEntitySlot = _container.EnsureContainer<ContainerSlot>(uid, SlotName);
    }

    private void OnMaskToggled(Entity<IdentityBlockerComponent> ent, ref ItemMaskToggledEvent args)
    {
        ent.Comp.Enabled = !args.IsToggled;
    }

    /// <summary>
    ///     When the identity of a person is changed, searches for a criminal records console and compares the name of
    ///     the new identity with a dictionary stored on the console. If the new name has a criminal status attached to it,
    ///     the person will get the same criminal status until they change identity again.
    /// </summary>
    private void OnIdentityChanged(Entity<IdentityComponent> ent, ref IdentityChangedEvent args)
    {
        var query = EntityQueryEnumerator<CriminalRecordsConsoleComponent>();
        var name = Identity.Name(ent, _entityManager);
        while (query.MoveNext(out var uid, out var criminalRecordsConsole))
        {
            if (criminalRecordsConsole.Criminals.TryGetValue(name, out var criminal))
                _criminalRecordsConsole.SetCriminalIcon(name, criminal,ent);
            else
                RemComp<CriminalRecordComponent>(ent);
            break;
        }
    }
}
/// <summary>
///     Gets called whenever an entity changes their identity.
/// </summary>
[ByRefEvent]
public record struct IdentityChangedEvent(EntityUid CharacterEntity, EntityUid IdentityEntity);
