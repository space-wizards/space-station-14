using Content.Server.Administration.Logs;
using Content.Server.Destructible;
using Content.Server.Popups;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Crayon;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Server.Crayon;

public sealed partial class FakeConsumableSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private IngestionSystem _ingestion = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
 
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FakeConsumableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<FakeConsumableComponent, FakeConsumableDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<FakeConsumableComponent, DamageThresholdReached>(OnDamageThresholdReached);
        SubscribeLocalEvent<FakeConsumableComponent, FullyEatenEvent>(OnEaten);
        SubscribeLocalEvent<FakeConsumableComponent, LandEvent>(OnLand);
    }

    private void OnInteractUsing(Entity<FakeConsumableComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var used = args.Used;
        var user = args.User;
        var slot = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.ContainerId);

        if (slot.ContainedEntity != null)
        {
            _popup.PopupCursor(Loc.GetString("fake-consumable-already-contained", ("contained", slot.ContainedEntity), ("owner", ent)), user);
            return;
        }

        if (_whitelist.IsWhitelistPass(ent.Comp.Blacklist, used))
        {
            _popup.PopupCursor(Loc.GetString("fake-consumable-blacklisted-item", ("used", used), ("owner", ent)), user);
            return;
        }

        var doAfterEventArgs = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(0.5f),
            new FakeConsumableDoAfterEvent(), ent, used: used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };
        _doAfter.TryStartDoAfter(doAfterEventArgs);

        _adminLog.Add(Shared.Database.LogType.InteractUsing, Shared.Database.LogImpact.Medium, $"{ToPrettyString(user):user} inserted {ToPrettyString(used):used} into {ToPrettyString(ent):target}");
        args.Handled = true;
    }

    private void OnDoAfter(Entity<FakeConsumableComponent> ent, ref FakeConsumableDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used == null)
            return;

        var used = args.Used.Value;
        var slot = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.ContainerId);
        _container.Insert(used, slot);
    }

    private void OnEaten(Entity<FakeConsumableComponent> ent, ref FullyEatenEvent args)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            return;

        if (container.Count == 0)
        {
            RevealContained(ent);
            return;
        }

        var user = args.User;
        var contained = container.ContainedEntities[0];

        if (!_ingestion.CanIngest(user, contained) || !ent.Comp.IngestContained)
            RevealContained(ent);
        else
            RevealContained(ent, true);
    }

    private void OnDamageThresholdReached(Entity<FakeConsumableComponent> ent, ref DamageThresholdReached args)
    {
        RevealContained(ent);
    }

    private void OnLand(Entity<FakeConsumableComponent> ent, ref LandEvent args)
    {
        RevealContained(ent);
    }

    private EntityUid? RevealContained(Entity<FakeConsumableComponent> ent, bool eat = false)
    {
        var coords = Transform(ent).Coordinates;

        _audio.PlayPvs(ent.Comp.OnVanishSound, coords);
        _popup.PopupCoordinates(Loc.GetString("fake-consumable-vanish", ("owner", ent)), coords);

        if (_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container)
            && container.Count == 1)
        {
            var contained = container.ContainedEntities[0];
            _container.Remove(contained, container);
            QueueDel(ent);

            if (eat)
            {
                // TODO -- Place the thing in your hand and eat it
            }

            return contained;
        }

        QueueDel(ent);
        return null;
    }
}
