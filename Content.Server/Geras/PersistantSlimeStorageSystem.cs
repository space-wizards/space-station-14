using System.Linq;
using Content.Server.Administration.Logs;
using Content.Shared.Coordinates;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Localizations;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Strip;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Server.Geras;

/// <summary>
/// Uses for transfering items between forms that share persistantSlimeStorage components.
/// Also shows items contained in storages on examine
/// </summary>
public sealed class PersistantSlimeStorageSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedStrippableSystem _strippableSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PersistantSlimeStorageComponent, PolymorphedEvent>(OnPolymorph);
        SubscribeLocalEvent<PersistantSlimeStorageComponent, ExaminedEvent>(OnExamine);
    }

    /// <summary>
    /// Gets contained items and adds them to examine
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="args"></param>
    private void OnExamine(Entity<PersistantSlimeStorageComponent> ent, ref ExaminedEvent args)
    {
        if (!_entityManager.TryGetComponent<StorageComponent>(ent, out var storage))
            return;
        var internalItemNames = storage.Container.ContainedEntities
            .Where(entity => !HasComp<VirtualItemComponent>(entity))
            .Select(item => FormattedMessage.EscapeText(Identity.Name(item, EntityManager)))
            .Select(itemName => Loc.GetString("comp-hands-examine-wrapper", ("item", itemName)))
            .ToList();

        if (internalItemNames.Count == 0)
            return;

        var locUser = ("user", Identity.Entity(ent, EntityManager));
        var locItems = ("items", ContentLocalizationManager.FormatList(internalItemNames));
        using (args.PushGroup(nameof(PersistantSlimeStorageComponent)))
        {
            args.PushMarkup(Loc.GetString("slime-examine-internalstorage", locUser, locItems));
        }
    }

    /// <summary>
    /// Tries to transfer items between polymorphs
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="args"></param>
    private void OnPolymorph(Entity<PersistantSlimeStorageComponent> ent, ref PolymorphedEvent args)
    {
        if (!_entityManager.TryGetComponent<StorageComponent>(ent, out var storage))
            return;
        if (!HasComp<PersistantSlimeStorageComponent>(args.NewEntity) || !_entityManager.HasComponent<StorageComponent>(args.NewEntity))
        {
            // Drop our internal inventory if this is a revert and the entity we are becoming does not have internal storage
            if (!args.IsRevert)
                return;

            _containerSystem.EmptyContainer(storage.Container, true, destination: null);
            return;
        }

        _storage.TransferEntities(ent, args.NewEntity);

        //Empty if we still have items
        if(storage.Container.ContainedEntities.Count != 0)
        {
            _containerSystem.EmptyContainer(storage.Container, true, destination: null);
        }
    }
}

