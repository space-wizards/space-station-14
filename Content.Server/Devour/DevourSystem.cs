using Content.Shared.Devour;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Content.Server.Body.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Chemistry.Components;
using Content.Shared.Actions;
using Robust.Shared.Containers;
using Content.Shared.Popups;
using Content.Server.Devour.Components;

namespace Content.Server.Devour;

public sealed class DevourSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DevourerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<DevourerComponent, DevourActionEvent>(OnDevourAction);
        SubscribeLocalEvent<DevourerComponent, DevourDoAfterEvent>(OnDoAfter);
    }

    private void OnDoAfter(EntityUid uid, DevourerComponent component, DevourDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        var ichorInjection = new Solution(component.Chemical, component.HealRate);

        if (component.FoodPreference == FoodPreference.All ||
            (component.FoodPreference == FoodPreference.Humanoid && HasComp<HumanoidAppearanceComponent>(args.Args.Target)))
        {
            ichorInjection.ScaleSolution(0.5f);

            if (component.ShouldStoreDevoured && args.Args.Target is not null)
            {
                component.Stomach.Insert(args.Args.Target.Value);
            }
            _bloodstreamSystem.TryAddToChemicals(uid, ichorInjection);
        }

        //TODO: Figure out a better way of removing structures via devour that still entails standing still and waiting for a DoAfter. Somehow.
        //If it's not human, it must be a structure
        else if (args.Args.Target != null)
            EntityManager.QueueDeleteEntity(args.Args.Target.Value);

        _audioSystem.PlayPvs(component.SoundDevour, uid);
    }

    private void OnStartup(EntityUid uid, DevourerComponent component, ComponentStartup args)
    {
        //Dragon doesn't actually chew, since he sends targets right into his stomach.
        //I did it mom, I added ERP content into upstream. Legally!
        component.Stomach = _containerSystem.EnsureContainer<Container>(uid, "dragon_stomach");

        if (component.DevourAction != null)
            _actionsSystem.AddAction(uid, component.DevourAction, null);
    }

    /// <summary>
    /// The devour action
    /// </summary>
    private void OnDevourAction(EntityUid uid, DevourerComponent component, DevourActionEvent args)
    {
        if (args.Handled || component.Whitelist?.IsValid(args.Target, EntityManager) != true)
            return;

        args.Handled = true;
        var target = args.Target;

        // Structure and mob devours handled differently.
        if (TryComp(target, out MobStateComponent? targetState))
        {
            switch (targetState.CurrentState)
            {
                case MobState.Critical:
                case MobState.Dead:

                    _doAfterSystem.TryStartDoAfter(new DoAfterArgs(uid, component.DevourTime, new DevourDoAfterEvent(), uid, target: target, used: uid)
                    {
                        BreakOnTargetMove = true,
                        BreakOnUserMove = true,
                    });
                    break;
                default:
                    _popupSystem.PopupEntity(Loc.GetString("devour-action-popup-message-fail-target-alive"), uid, uid);
                    break;
            }

            return;
        }

        _popupSystem.PopupEntity(Loc.GetString("devour-action-popup-message-structure"), uid, uid);

        if (component.SoundStructureDevour != null)
            _audioSystem.PlayPvs(component.SoundStructureDevour, uid, component.SoundStructureDevour.Params);

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(uid, component.StructureDevourTime, new DevourDoAfterEvent(), uid, target: target, used: uid)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
        });
    }
}
