using Content.Shared.Actions;
using Content.Shared.Body.Events;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Devour.Components;
using Content.Shared.DoAfter;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared.Devour;

public abstract class SharedDevourSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DevourerComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<DevourerComponent, DevourActionEvent>(OnDevourAction);
        SubscribeLocalEvent<DevourerComponent, DevourDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<DevourerComponent, BeingGibbedEvent>(OnGibContents);
    }

    private void OnInit(EntityUid uid, DevourerComponent component, MapInitEvent args)
    {
        //Devourer doesn't actually chew, since he sends targets right into his stomach.
        //I did it mom, I added ERP content into upstream. Legally!
        component.Stomach = _containerSystem.EnsureContainer<Container>(uid, "stomach");

        _actionsSystem.AddAction(uid, ref component.DevourActionEntity, component.DevourAction);
    }

    /// <summary>
    /// The devour action
    /// </summary>
    private void OnDevourAction(EntityUid uid, DevourerComponent component, DevourActionEvent args)
    {
        if (args.Handled || _whitelistSystem.IsWhitelistFailOrNull(component.Whitelist, args.Target))
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

                    _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, component.DevourTime, new DevourDoAfterEvent(), uid, target: target, used: uid)
                    {
                        BreakOnMove = true,
                    });
                    break;
                default:
                    _popupSystem.PopupClient(Loc.GetString("devour-action-popup-message-fail-target-alive"), uid,uid);
                    break;
            }

            return;
        }

        _popupSystem.PopupClient(Loc.GetString("devour-action-popup-message-structure"), uid, uid);

        if (component.SoundStructureDevour != null)
            _audioSystem.PlayPredicted(component.SoundStructureDevour, uid, uid, component.SoundStructureDevour.Params);

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, component.StructureDevourTime, new DevourDoAfterEvent(), uid, target: target, used: uid)
        {
            BreakOnMove = true,
        });
    }

    private void OnDoAfter(EntityUid uid, DevourerComponent component, DevourDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        var ichorInjection = new Solution(component.Chemical, component.HealRate);

        // Grant ichor if the devoured thing meets the dragon's food preference
        if (args.Args.Target != null && _whitelistSystem.IsWhitelistPassOrNull(component.FoodPreferenceWhitelist, (EntityUid)args.Args.Target))
        {
            _bloodstreamSystem.TryAddToChemicals(uid, ichorInjection);
        }

        // If the devoured thing meets the stomach whitelist criteria, add it to the stomach
        if (args.Args.Target != null && _whitelistSystem.IsWhitelistPass(component.StomachStorageWhitelist, (EntityUid)args.Args.Target))
        {
            _containerSystem.Insert(args.Args.Target.Value, component.Stomach);
        }
        //TODO: Figure out a better way of removing structures via devour that still entails standing still and waiting for a DoAfter. Somehow.
        //If it's not alive, it must be a structure.
        // Delete if the thing isn't in the stomach storage whitelist (or the stomach whitelist is null/empty)
        else if (args.Args.Target != null)
        {
            QueueDel(args.Args.Target.Value);
        }

        _audioSystem.PlayPvs(component.SoundDevour, uid);
    }

    private void OnGibContents(EntityUid uid, DevourerComponent component, ref BeingGibbedEvent args)
    {
        if (component.StomachStorageWhitelist == null)
            return;

        // For some reason we have two different systems that should handle gibbing,
        // and for some another reason GibbingSystem, which should empty all containers, doesn't get involved in this process
        _containerSystem.EmptyContainer(component.Stomach);
    }
}

public sealed partial class DevourActionEvent : EntityTargetActionEvent;

[Serializable, NetSerializable]
public sealed partial class DevourDoAfterEvent : SimpleDoAfterEvent;

