using Content.Server.DoAfter;
using Content.Shared.Rocks;
using Content.Shared.Interaction;
using Content.Shared.DoAfter;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;

namespace Content.Server.Rocks;

public sealed partial class FlintRockSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom Random = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FlintRockComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<FlintRockComponent, CollectFlintDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<FlintRockComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<FlintRockComponent, ExaminedEvent>(OnExamined);
    }

    private void OnMapInit(EntityUid uid, FlintRockComponent component, MapInitEvent args)
    {
        component.CurrentFlints = Random.Next(1, component.MaxFlints + 1);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FlintRockComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            var currentTime = _gameTiming.CurTime;
            if (currentTime >= component.LastRegenerationTime + TimeSpan.FromHours(component.RegenerationTime))
            {
                if (component.CurrentFlints < component.MaxFlints)
                {
                    component.CurrentFlints++;
                    component.LastRegenerationTime = currentTime;
                }
            }
        }
    }

    private void OnGetVerbs(EntityUid uid, FlintRockComponent component, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || component.CurrentFlints <= 0)
            return;

        var user = args.User;

        var verb = new AlternativeVerb
        {
            Text = "Collect Flint",
            Act = () => StartCollectingFlint(uid, component, user)
        };
        args.Verbs.Add(verb);
    }

    private void StartCollectingFlint(EntityUid rockUid, FlintRockComponent component, EntityUid user)
    {
        var doAfterArgs = new DoAfterArgs(EntityManager, user, component.CollectionTime, new CollectFlintDoAfterEvent(), rockUid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnDoAfter(EntityUid uid, FlintRockComponent component, ref CollectFlintDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        component.CurrentFlints--;
        var spawnPos = Transform(uid).MapPosition;
        var flint = Spawn("Flint", spawnPos);
        _hands.TryPickupAnyHand(args.Args.User, flint);
        _popup.PopupEntity("You successfully collect a flint.", uid, args.Args.User);
        args.Handled = true;
    }

    private void OnExamined(EntityUid uid, FlintRockComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var flintCount = component.CurrentFlints;
        if (flintCount > 0)
        {
            var message = flintCount == 1
                ? "A single piece of flint is partially exposed in the rock."
                : $"There are {flintCount} loose pieces of flint in the rock.";
            args.PushMarkup(message);
        }
    }
}