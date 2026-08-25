using Content.Server.Administration.Logs;
using Content.Server.Destructible;
using Content.Server.Popups;
using Content.Shared.Containers;
using Content.Shared.Crayon;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Nutrition;
using Content.Shared.Throwing;
using Content.Shared.Trigger.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Crayon;

public sealed partial class FakeConsumableSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();


        SubscribeLocalEvent<FakeConsumableComponent, FakeConsumableDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<FakeConsumableComponent, InteractUsingEvent>(OnInteractUsingEvent);
        SubscribeLocalEvent<FakeConsumableComponent, DamageThresholdReached>(OnDamageThresholdReached);
        SubscribeLocalEvent<FakeConsumableComponent, LandEvent>(OnLandEvent);
        SubscribeLocalEvent<FakeConsumableComponent, IngestedEvent>(OnIngested);
    }


    private void OnInteractUsingEvent(Entity<FakeConsumableComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var used = args.Used;
        var user = args.User;

        if (ent.Comp.Contained != null)
        {
            _popup.PopupCursor(Loc.GetString("fake-consumable-already-contained", ("contained", ent.Comp.Contained), ("owner", ent)), user);
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

        ent.Comp.Contained = MetaData(used).EntityPrototype?.ID;
        QueueDel(used);
    }

    private void OnDamageThresholdReached(Entity<FakeConsumableComponent> ent, ref DamageThresholdReached args)
    {
        RevealContained(ent, null);
    }

    private void OnLandEvent(Entity<FakeConsumableComponent> ent, ref LandEvent args)
    {
        RevealContained(ent, args.User);
    }

    private void RevealContained(Entity<FakeConsumableComponent> ent, EntityUid? user)
    {
        var landingCoords = Transform(ent).Coordinates;
        _audio.PlayPvs(ent.Comp.OnVanishSound, landingCoords);
        _popup.PopupCoordinates(Loc.GetString("fake-consumable-vanish", ("owner", ent)), landingCoords);

        QueueDel(ent);

        if (ent.Comp.Contained == null)
            return;

        var contained = Spawn(ent.Comp.Contained, landingCoords);
        _trigger.ActivateTimerTrigger(contained, user);
    }
}
