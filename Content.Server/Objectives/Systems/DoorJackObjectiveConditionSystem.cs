using Content.Server.Objectives.Components;
using Content.Shared.Doors.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Content.Shared.Popups;

namespace Content.Server.Objectives.Systems;

public sealed partial class DoorJackObjectiveConditionSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private CounterConditionSystem _counterCondition = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpaceNinjaComponent, EmaggedSomethingEvent>(OnDoorjack);
    }

    private void OnDoorjack(EntityUid uid, SpaceNinjaComponent comp, ref EmaggedSomethingEvent args)
    {
        // incase someone lets ninja emag non-doors double check it here
        if (!HasComp<DoorComponent>(args.Target))
            return;

        if (!_mind.TryGetMind(uid, out var mindUid, out var mind))
            return;

        // this popup is serverside since door emag logic is serverside (power funnies)
        _popupSystem.PopupEntity(
            Loc.GetString("ninja-doorjack-success", ("target", Identity.Entity(args.Target, EntityManager))),
            uid,
            uid,
            PopupType.Medium);

        foreach (var obj in _mind.EnumerateObjectives<DoorjackConditionComponent>((mindUid, mind)))
        {
            _counterCondition.IncreaseCount(obj);
        }
    }
}
