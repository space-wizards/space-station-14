using Content.Shared.Administration.Logs;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Holiday.Christmas;

/// <summary>
///     System for granting players a totally random item when using an entity.
/// </summary>
public sealed class RandomGiftSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly List<string> _possibleGiftsSafe = new(); // Should these be HashSet?
    private readonly List<string> _possibleGiftsUnsafe = new();

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<RandomGiftComponent, MapInitEvent>(OnGiftMapInit);
        SubscribeLocalEvent<RandomGiftComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<RandomGiftComponent, ExaminedEvent>(OnExamined);
        BuildIndex();
    }

    /// <summary>
    ///     Santa can peek inside the present.
    /// </summary>
    private void OnExamined(Entity<RandomGiftComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.SelectedEntity is not { } spawnId
            || _whitelistSystem.IsWhitelistFail(ent.Comp.ContentsViewers, args.Examiner))
            return;

        var name = _prototype.Index(spawnId).Name;
        args.PushText(Loc.GetString(ent.Comp.GiftContains, ("name", name)));
    }

    /// <summary>
    ///     Open the present.
    /// </summary>
    private void OnUseInHand(Entity<RandomGiftComponent> ent, ref UseInHandEvent args)
    {
        var (gift, comp) = ent;

        if (args.Handled || comp.SelectedEntity is null)
            return;

        var spawned = SpawnNextToOrDrop(comp.SelectedEntity, gift);
        _adminLogger.Add(LogType.EntitySpawn, LogImpact.Low,
            $"{ToPrettyString(args.User)} used {ToPrettyString(gift)} which spawned {ToPrettyString(spawned)}");

        if (comp.Wrapper is { } trash)
            SpawnNextToOrDrop(trash, gift);

        // Play sound at the spawned entity instead of the gift since it's going to get deleted
        _audio.PlayPvs(comp.Sound, spawned);

        // Don't delete the entity in the event bus, so we queue it for deletion.
        // We need the free hand for the new item, so we send it to nullspace.
        _transform.DetachEntity(gift, Transform(gift));
        QueueDel(gift);

        _hands.PickupOrDrop(args.User, spawned);

        args.Handled = true;
    }

    // TODO move to shared once this is predicted
    /// <summary>
    ///     Pre-select the contained entity.
    /// </summary>
    private void OnGiftMapInit(Entity<RandomGiftComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.SelectedEntity = _random.Pick(ent.Comp.InsaneMode ? _possibleGiftsUnsafe : _possibleGiftsSafe);
    }

    /// <summary>
    ///     Rebuild the entity lists.
    /// </summary>
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs obj)
    {
        if (obj.WasModified<EntityPrototype>())
            BuildIndex();
    }

    /// <summary>
    ///     Builds a safe list and unsafe list from all <see cref="EntityPrototype"/>s.
    /// </summary>
    private void BuildIndex()
    {
        _possibleGiftsSafe.Clear();
        _possibleGiftsUnsafe.Clear();
        var itemCompName = Factory.GetComponentName<ItemComponent>();
        var mapGridCompName = Factory.GetComponentName<MapGridComponent>();
        var physicsCompName = Factory.GetComponentName<PhysicsComponent>();

        foreach (var proto in _prototype.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.Abstract || // it's not real
                proto.HideSpawnMenu || // it's too weird
                proto.Components.ContainsKey(mapGridCompName) || // it's too big
                !proto.Components.ContainsKey(physicsCompName)) // it just wouldn't work well
                continue;

            _possibleGiftsUnsafe.Add(proto.ID);

            if (!proto.Components.ContainsKey(itemCompName))
                continue;

            _possibleGiftsSafe.Add(proto.ID);
        }
    }
}
