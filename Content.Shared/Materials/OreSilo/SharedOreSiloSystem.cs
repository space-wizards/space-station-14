using Content.Shared.Power.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Shared.Materials.OreSilo;

public abstract class SharedOreSiloSystem : EntitySystem
{
    [Dependency] private readonly SharedMaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<OreSiloClientComponent> _clientQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<OreSiloComponent, ToggleOreSiloClientMessage>(OnToggleOreSiloClient);
        SubscribeLocalEvent<OreSiloComponent, ComponentShutdown>(OnSiloShutdown);
        Subs.BuiEvents<OreSiloComponent>(OreSiloUiKey.Key,
            subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnBoundUIOpened);
        });


        SubscribeLocalEvent<OreSiloClientComponent, GetStoredMaterialsEvent>(OnGetStoredMaterials);
        SubscribeLocalEvent<OreSiloClientComponent, ConsumeStoredMaterialsEvent>(OnConsumeStoredMaterials);
        SubscribeLocalEvent<OreSiloClientComponent, ComponentShutdown>(OnClientShutdown);

        _clientQuery = GetEntityQuery<OreSiloClientComponent>();
    }

    private void OnToggleOreSiloClient(Entity<OreSiloComponent> ent, ref ToggleOreSiloClientMessage args)
    {
        var client = GetEntity(args.Client);

        if (!_clientQuery.TryComp(client, out var clientComp))
            return;

        if (ent.Comp.Clients.Contains(client)) // remove client
        {
            clientComp.Silo = null;
            Dirty(client, clientComp);
            ent.Comp.Clients.Remove(client);
            Dirty(ent);

            UpdateOreSiloUi(ent);
        }
        else // add client
        {
            if (!CanTransmitMaterials((ent, ent), client))
                return;

            var clientMats = _materialStorage.GetStoredMaterials(client, true);
            var inverseMats = new Dictionary<string, int>();
            foreach (var (mat, amount) in clientMats)
            {
                inverseMats.Add(mat, -amount);
            }
            _materialStorage.TryChangeMaterialAmount(client, inverseMats, localOnly: true);
            _materialStorage.TryChangeMaterialAmount(ent.Owner, clientMats);

            ent.Comp.Clients.Add(client);
            Dirty(ent);
            clientComp.Silo = ent;
            Dirty(client, clientComp);

            UpdateOreSiloUi(ent);
        }
    }

    private void OnBoundUIOpened(Entity<OreSiloComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateOreSiloUi(ent);
    }

    private void OnSiloShutdown(Entity<OreSiloComponent> ent, ref ComponentShutdown args)
    {
        foreach (var client in ent.Comp.Clients)
        {
            if (!_clientQuery.TryComp(client, out var comp))
                continue;

            comp.Silo = null;
            Dirty(client, comp);
        }
    }

    protected virtual void UpdateOreSiloUi(Entity<OreSiloComponent> ent)
    {

    }

    private void OnGetStoredMaterials(Entity<OreSiloClientComponent> ent, ref GetStoredMaterialsEvent args)
    {
        if (args.LocalOnly)
            return;

        if (ent.Comp.Silo is not { } silo)
            return;

        if (!CanTransmitMaterials(silo, ent))
            return;

        var materials = _materialStorage.GetStoredMaterials(silo);

        foreach (var (mat, amount) in materials)
        {
            // Don't supply materials that they don't usually have access to.
            if (!_materialStorage.IsMaterialWhitelisted((args.Entity, args.Entity), mat))
                continue;

            var existing = args.Materials.GetOrNew(mat);
            args.Materials[mat] = existing + amount;
        }
    }

    private void OnConsumeStoredMaterials(Entity<OreSiloClientComponent> ent, ref ConsumeStoredMaterialsEvent args)
    {
        if (args.LocalOnly)
            return;

        if (ent.Comp.Silo is not { } silo || !TryComp<MaterialStorageComponent>(silo, out var materialStorage))
            return;

        if (!CanTransmitMaterials(silo, ent))
            return;

        foreach (var (mat, amount) in args.Materials)
        {
            if (!_materialStorage.TryChangeMaterialAmount(silo, mat, amount, materialStorage))
                continue;
            args.Materials[mat] = 0;
        }
    }

    private void OnClientShutdown(Entity<OreSiloClientComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<OreSiloComponent>(ent.Comp.Silo, out var silo))
            return;

        silo.Clients.Remove(ent);
        Dirty(ent.Comp.Silo.Value, silo);
        UpdateOreSiloUi((ent.Comp.Silo.Value, silo));
    }

    /// <summary>
    /// Checks if a given client fulfills the criteria to link/receive materials from an ore silo.
    /// </summary>
    [PublicAPI]
    public bool CanTransmitMaterials(Entity<OreSiloComponent?, TransformComponent?> silo, EntityUid client)
    {
        if (!Resolve(silo, ref silo.Comp1, ref silo.Comp2))
            return false;

        if (!_powerReceiver.IsPowered(silo.Owner))
            return false;

        if (_transform.GetGrid(client) != _transform.GetGrid(silo.Owner))
            return false;

        if (!_transform.InRange((silo.Owner, silo.Comp2), client, silo.Comp1.Range))
            return false;

        return true;
    }
}
