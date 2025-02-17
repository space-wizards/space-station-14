using Content.Shared.Actions;
using Content.Shared.Popups;
using Content.Shared.DoAfter;
using Robust.Shared.Containers;
using Content.Server.DeadSpace.Abilities.Cocoon.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using System.Linq;
using Content.Shared.NPC.Systems;
using Content.Shared.DeadSpace.Abilities.Cocoon;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;

namespace Content.Server.DeadSpace.Abilities.Cocoon;

public sealed class LockCocoonSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly SharedCuffableSystem _cuffs = default!;
    [Dependency] private readonly CocoonSystem _cocoon = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LockCocoonComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<LockCocoonComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<LockCocoonComponent, LockCocoonEvent>(OnLockCocoon);
        SubscribeLocalEvent<LockCocoonComponent, LockCocoonDoAfterEvent>(OnDoAfter);
    }

    private void OnComponentInit(EntityUid uid, LockCocoonComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.LockCocoonEntity, component.LockCocoon, uid);
    }

    private void OnComponentShutdown(EntityUid uid, LockCocoonComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.LockCocoonEntity);
    }

    private void OnLockCocoon(EntityUid uid, LockCocoonComponent component, LockCocoonEvent args)
    {
        if (args.Handled)
            return;

        if (!IsCanInsertCocoon(uid, args.Target))
            return;

        if (args.Target == args.Performer)
            return;

        var searchDoAfter = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(component.Duration), new LockCocoonDoAfterEvent(), uid, target: args.Target)
        {
            Broadcast = true,
            DistanceThreshold = 2,
            BreakOnMove = true
        };

        if (!_doAfter.TryStartDoAfter(searchDoAfter))
            return;
    }

    private void OnDoAfter(EntityUid uid, LockCocoonComponent component, LockCocoonDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        var target = args.Args.Target.Value;

        if (!IsCanInsertCocoon(uid, target))
            return;

        if (component.NeedHandcuff)
        {
            if (HasComp<HumanoidAppearanceComponent>(target))
            {
                var handcuff = Spawn(component.HandcuffsProtorype, Transform(uid).Coordinates);

                if (!TryComp<HandcuffComponent>(handcuff, out var handcuffComponent) || !handcuffComponent.Used)
                {
                    Del(handcuff);
                    return;
                }

                if (!_cuffs.TryAddNewCuffs(target, target, handcuff))
                    Del(handcuff);
            }
        }

        var cocoon = Spawn(component.Cocoon, Transform(target).Coordinates);

        if (!TryComp<CocoonComponent>(cocoon, out var cocoonComponent))
            return;

        _cocoon.TryInsertCocoon(cocoon, target, cocoonComponent);
    }

    public bool IsCanInsertCocoon(EntityUid uid, EntityUid target, LockCocoonComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        bool isCanSuck = false;

        if (!TryComp<MobStateComponent>(target, out var mobState))
            return false;

        var stateTranslations = new Dictionary<MobState, string>
        {
            { MobState.Invalid, "недействительное" },
            { MobState.Alive, "живое" },
            { MobState.Critical, "критическое" },
            { MobState.Dead, "мёртвое" }
        };

        foreach (var allowedState in component.AllowedStates)
        {
            if (allowedState == mobState.CurrentState)
            {
                isCanSuck = true;
                break;
            }
        }

        if (!isCanSuck)
        {
            var allowedStatesString = string.Join(", ", component.AllowedStates.Select(state => stateTranslations[state]));
            _popup.PopupEntity(Loc.GetString($"Цель должна быть в одном из следующих состояний: {allowedStatesString}"), uid, uid);
            return false;
        }

        if (component.IsHumanoid)
        {
            if (!HasComp<HumanoidAppearanceComponent>(target))
            {
                _popup.PopupEntity(Loc.GetString("Цель должна быть гуманоидом!"), uid, uid);
                return false;
            }
        }

        if (!component.IgnorFriends)
        {
            if (_npcFaction.IsEntityFriendly(uid, target))
            {
                _popup.PopupEntity(Loc.GetString("Вы не можете сделать это с союзником!"), uid, uid);
                return false;
            }
        }

        return true;
    }

}
