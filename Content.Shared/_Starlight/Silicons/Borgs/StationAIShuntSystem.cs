using Content.Shared._Starlight.Polymorph.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Follower;
using Content.Shared.Follower.Components;
using Content.Shared.Mind;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared._Starlight.Silicons.Borgs;

public sealed class StationAIShuntSystem : EntitySystem
{

    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedSiliconLawSystem _siliconLaw = default!;
    [Dependency] private readonly FollowerSystem _follower = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAIShuntableComponent, AIShuntActionEvent>(OnAttemptShunt);

        SubscribeLocalEvent<StationAIShuntComponent, AIUnShuntActionEvent>(OnAttemptUnshunt);
        SubscribeLocalEvent<StationAIShuntComponent, GetVerbsEvent<AlternativeVerb>>(GetAltVerbs);

    }

    #region Actions
    private void OnAttemptShunt(EntityUid uid, StationAIShuntableComponent shuntable, AIShuntActionEvent ev)
    {
        if (ev.Handled)
            return;
        var target = ev.Target;

        if (!TryComp<StationAIShuntComponent>(target, out var shunt))
            return;
        if (!_mindSystem.TryGetMind(uid, out var mindId, out var _))
            return;

        if (TryComp<BorgChassisComponent>(target, out var chassisComp))
        {
            var brain = chassisComp.BrainContainer.ContainedEntity;
            if (!brain.HasValue)
                return; //Chassis has no posibrian so cant shunt into it.
            if (!TryComp<StationAIShuntComponent>(brain, out var brainShunt))
                return; //Chassis brain is not able to be shunted into so obviously we cant.
            brainShunt.Return = uid;
            brainShunt.ReturnAction = _actionSystem.AddAction(brain.Value, shuntable.UnshuntAction.Id);
        }

        shunt.Return = uid;
        _mindSystem.TransferTo(mindId, target);
        shunt.ReturnAction = _actionSystem.AddAction(target, shuntable.UnshuntAction.Id);
        shuntable.Inhabited = target;

        if (TryComp<SiliconLawProviderComponent>(uid, out var coreLaws))
        {
            var getLaws = new GetSiliconLawsEvent(target);
            RaiseLocalEvent(target, ref getLaws);
            shunt.OldLawset = getLaws.Laws;

            _siliconLaw.SetLawset(target, coreLaws.Lawset);
        }

        EnsureComp<UncryoableComponent>(uid);

        var core = Transform(uid).ParentUid;
        if (TryComp<StationAiCoreComponent>(core, out var coreComp) &&
            TryComp<FollowedComponent>(coreComp.RemoteEntity, out var followed)
        )
        {
            foreach (var follower in followed.Following)
            {
                _follower.StartFollowingEntity(follower, target);
            }
        }

        ev.Handled = true;
    }

    private void OnAttemptUnshunt(EntityUid uid, StationAIShuntComponent shunt, AIUnShuntActionEvent ev)
    {
        if (ev.Handled)
            return;

        if (!_mindSystem.TryGetMind(uid, out var mindId, out var _))
            return;

        if (!TryComp<ActionComponent>(shunt.ReturnAction, out var act))
            return; //Somehow the action does not have action component? invalid perhaps?

        if (!TryComp<StationAIShuntableComponent>(shunt.Return, out var shuntable))
            return; //trying to return to a body you cant leave from? weird...

        if (TryComp<BorgChassisComponent>(uid, out var chassisComp))
        {
            var brain = chassisComp.BrainContainer.ContainedEntity;
            if (!brain.HasValue)
                return; //Chassis has no brain... how is the AI controlling it???
            if (!TryComp<StationAIShuntComponent>(brain, out var brainShunt))
                return; //Chassis brain is not able to be shunted into so how is AI controlling it???
            if (!TryComp<ActionComponent>(brainShunt.ReturnAction, out var brainAct))
                return; //Somehow the action does not have action component? invalid perhaps?
            _actionSystem.RemoveAction(new Entity<ActionComponent?>(brainShunt.ReturnAction.Value, brainAct));
            brainShunt.Return = null; //cause we are returning now
            brainShunt.ReturnAction = null;
        }

        _actionSystem.RemoveAction(new Entity<ActionComponent?>(shunt.ReturnAction.Value, act));
        var target = shunt.Return.Value;
        _mindSystem.TransferTo(mindId, target);
        RemComp<UncryoableComponent>(target);

        var aiCore = Transform(target).ParentUid;
        if (TryComp<StationAiCoreComponent>(aiCore, out var core) &&
            core.RemoteEntity.HasValue
            )
        {
            _transform.SetMapCoordinates(core.RemoteEntity.Value,
                _transform.ToMapCoordinates(Transform(uid).Coordinates)
            );

            if (TryComp<FollowedComponent>(uid, out var followed))
            {
                foreach (var follower in followed.Following)
                {
                    _follower.StartFollowingEntity(follower, core.RemoteEntity.Value);
                }
            }
        }

        _siliconLaw.SetLawset(uid, shunt.OldLawset);

        shunt.ReturnAction = null;
        shunt.Return = null;
        shuntable.Inhabited = null;
    }
    #endregion

    #region Verbs
    public void GetAltVerbs(EntityUid uid, StationAIShuntComponent comp, GetVerbsEvent<AlternativeVerb> ev)
    {
        if (ev.User == ev.Target) //if we are targeting outselves
        {
            if (!comp.Return.HasValue)
                return; //we are in something not inhabited. so obvs we cant shunt out of it.
            
            var unshuntVerb = new AlternativeVerb()
            {
                Text = Loc.GetString("ai-shunt-out-of"),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/vv.svg.192dpi.png")),
                Act = () =>
                {
                    var shuntEv = new AIUnShuntActionEvent()
                    {
                        Performer = uid
                    };
                    RaiseLocalEvent(uid, shuntEv);
                }
            };
            ev.Verbs.Add(unshuntVerb);
            return;
        }

        if (!HasComp<StationAIShuntableComponent>(ev.User))
            return; //only shuntable can get the into verb

        if (TryComp<BorgChassisComponent>(uid, out var chassis) && !HasComp<StationAIShuntComponent>(chassis.BrainContainer.ContainedEntity))
            return; //target borg chassis has no brain with shuntable component.

        var shuntVerb = new AlternativeVerb()
        {
            Text = Loc.GetString("ai-shunt-into"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/vv.svg.192dpi.png")),
            Act = () =>
            {
                var shuntEv = new AIShuntActionEvent()
                {
                    Target = uid,
                    Performer = ev.User
                };
                RaiseLocalEvent(ev.User, shuntEv);
            }
        };
        ev.Verbs.Add(shuntVerb);

    }
    #endregion
}

public sealed partial class AIShuntActionEvent : EntityTargetActionEvent
{
}

public sealed partial class AIUnShuntActionEvent : InstantActionEvent
{
}
