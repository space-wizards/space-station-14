using Content.Server.Botany;
using Content.Server.Botany.Systems;
using Content.Server.Botany.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.RussStation.Botany;
using Content.Shared.RussStation.Botany.Components;
using Content.Shared.RussStation.Botany.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server.RussStation.Botany.Systems;

public sealed class SeedExtractorStorageSystem : SharedSeedExtractorStorageSystem
{
    [Dependency] private readonly BotanySystem _botanySystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSys = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SeedExtractorStorageComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SeedExtractorStorageComponent, BeforeActivatableUIOpenEvent>((u, c, _) => UpdateUserInterfaceState(u, c));
        SubscribeLocalEvent<SeedExtractorStorageComponent, SeedExtractorStorageTakeSeedMessage>(OnTakeSeedMessage);
        SubscribeLocalEvent<SeedExtractorStorageComponent, EntInsertedIntoContainerMessage>(OnContainerChanged);
        SubscribeLocalEvent<SeedExtractorStorageComponent, EntRemovedFromContainerMessage>(OnContainerChanged);
    }

    /// <summary>
    /// When the player uses a seed packet on the extractor, store the packet inside the extractor.
    /// </summary>
    private void OnInteractUsing(EntityUid uid, SeedExtractorStorageComponent storage, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!this.IsPowered(uid, EntityManager))
            return;

        if (!TryComp(args.Used, out SeedComponent? _))
            return;

        var seedContainer = Container.EnsureContainer<Container>(uid, storage.SeedContainerId);
        args.Handled = Container.Insert(args.Used, seedContainer);
    }

    /// <summary>
    /// Takes one seed packet from the group identified by <paramref name="args"/>.<see cref="SeedExtractorStorageTakeSeedMessage.GroupKey"/>.
    /// </summary>
    private void OnTakeSeedMessage(EntityUid uid, SeedExtractorStorageComponent component, SeedExtractorStorageTakeSeedMessage args)
    {
        if (!Container.TryGetContainer(uid, component.SeedContainerId, out var seedContainer))
            return;

        foreach (var entity in seedContainer.ContainedEntities)
        {
            if (!TryComp(entity, out SeedComponent? seedComp))
                continue;

            if (!_botanySystem.TryGetSeed(seedComp, out var seed))
                continue;

            if (MakeGroupKey(seed) != args.GroupKey)
                continue;

            Container.Remove(entity, seedContainer);
            _hands.TryPickupAnyHand(args.Actor, entity);
            return;
        }
    }

    private void OnContainerChanged(EntityUid uid, SeedExtractorStorageComponent component, ContainerModifiedMessage args)
    {
        if (args.Container.ID != component.SeedContainerId)
            return;

        UpdateUserInterfaceState(uid, component);
    }

    private void UpdateUserInterfaceState(EntityUid uid, SeedExtractorStorageComponent component)
    {
        var seedDataList = new List<SeedExtractorStorageSeedData>();

        if (Container.TryGetContainer(uid, component.SeedContainerId, out var seedContainer))
        {
            var groups = new Dictionary<string, (SeedData data, string displayName, int count)>();

            foreach (var entity in seedContainer.ContainedEntities)
            {
                if (!TryComp(entity, out SeedComponent? seedComp))
                    continue;

                if (!_botanySystem.TryGetSeed(seedComp, out var seed))
                    continue;

                var key = MakeGroupKey(seed);
                if (groups.TryGetValue(key, out var existing))
                    groups[key] = (existing.data, existing.displayName, existing.count + 1);
                else
                {
                    var packetName = Loc.GetString("botany-seed-packet-name",
                        ("seedName", Loc.GetString(seed.Name)),
                        ("seedNoun", Loc.GetString(seed.Noun)));
                    groups[key] = (seed, packetName, 1);
                }
            }

            foreach (var (key, (data, displayName, count)) in groups)
            {
                seedDataList.Add(new SeedExtractorStorageSeedData
                {
                    DisplayName = displayName,
                    GroupKey = key,
                    PacketPrototype = data.PacketPrototype,
                    Count = count,
                    Potency = data.Potency,
                    Yield = data.Yield,
                    Endurance = data.Endurance,
                    Lifespan = data.Lifespan,
                    Maturation = data.Maturation,
                    Production = data.Production,
                });
            }
        }

        _uiSys.SetUiState(uid, SeedExtractorStorageUiKey.Key, new SeedExtractorStorageUpdateState(seedDataList));
    }

    /// <summary>
    /// Builds a composite key from all displayed stats so that seeds with any differing stat values
    /// are placed in separate groups regardless of sharing the same display name.
    /// </summary>
    private static string MakeGroupKey(SeedData seed)
    {
        return $"{seed.Name}|{seed.Potency:G}|{seed.Yield}|{seed.Endurance:G}|{seed.Lifespan:G}|{seed.Maturation:G}|{seed.Production:G}";
    }
}
