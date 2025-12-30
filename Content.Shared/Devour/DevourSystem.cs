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

public sealed class DevourSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DevourerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<DevourerComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<DevourerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<DevourerComponent, DevourActionEvent>(OnDevourAction);
        SubscribeLocalEvent<DevourerComponent, DevourDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<DevourerComponent, BeingGibbedEvent>(OnGibContents);
    }

    private void OnStartup(Entity<DevourerComponent> ent, ref ComponentStartup args)
    {
        //Devourer doesn't actually chew, since he sends targets right into his stomach.
        //I did it mom, I added ERP content into upstream. Legally!
        ent.Comp.Stomach = _containerSystem.EnsureContainer<Container>(ent.Owner, DevourerComponent.StomachContainerId);
    }

    private void OnInit(Entity<DevourerComponent> ent, ref MapInitEvent args)
    {
        _actionsSystem.AddAction(ent.Owner, ref ent.Comp.DevourActionEntity, ent.Comp.DevourAction);
    }

    private void OnShutdown(Entity<DevourerComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent.Owner, ent.Comp.DevourActionEntity);
    }

    /// <summary>
    /// The devour action
    /// </summary>
    private void OnDevourAction(Entity<DevourerComponent> ent, ref DevourActionEvent args)
    {
        if (args.Handled || _whitelistSystem.IsWhitelistFailOrNull(ent.Comp.Whitelist, args.Target))
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

                    _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, ent.Owner, ent.Comp.DevourTime, new DevourDoAfterEvent(), ent.Owner, target: target, used: ent.Owner)
                    {
                        BreakOnMove = true,
                    });
                    break;
                case MobState.Invalid:
                case MobState.Alive:
                default:
                    _popupSystem.PopupClient(Loc.GetString("devour-action-popup-message-fail-target-alive"), ent.Owner, ent.Owner);
                    break;
            }

            return;
        }

        _popupSystem.PopupClient(Loc.GetString("devour-action-popup-message-structure"), ent.Owner, ent.Owner);

        if (ent.Comp.SoundStructureDevour != null)
            _audioSystem.PlayPredicted(ent.Comp.SoundStructureDevour, ent.Owner, ent.Owner, ent.Comp.SoundStructureDevour.Params);

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, ent.Owner, ent.Comp.StructureDevourTime, new DevourDoAfterEvent(), ent.Owner, target: target, used: ent.Owner)
        {
            BreakOnMove = true,
        });
    }

    private void OnDoAfter(Entity<DevourerComponent> ent, ref DevourDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        var ichorInjection = new Solution(ent.Comp.Chemical, ent.Comp.HealRate);

        // Grant ichor if the devoured thing meets the dragon's food preference
        if (args.Args.Target != null && _whitelistSystem.IsWhitelistPassOrNull(ent.Comp.FoodPreferenceWhitelist, (EntityUid)args.Args.Target))
        {
            _bloodstreamSystem.TryAddToBloodstream(ent.Owner, ichorInjection);
        }

        // If the devoured thing meets the stomach whitelist criteria, add it to the stomach
        if (args.Args.Target != null && _whitelistSystem.IsWhitelistPass(ent.Comp.StomachStorageWhitelist, (EntityUid)args.Args.Target))
        {
            _containerSystem.Insert(args.Args.Target.Value, ent.Comp.Stomach);
        }
        //TODO: Figure out a better way of removing structures via devour that still entails standing still and waiting for a DoAfter. Somehow.
        //If it's not alive, it must be a structure.
        // Delete if the thing isn't in the stomach storage whitelist (or the stomach whitelist is null/empty)
        else if (args.Args.Target != null)
        {
            PredictedQueueDel(args.Args.Target.Value);
        }

        _audioSystem.PlayPredicted(ent.Comp.SoundDevour, ent.Owner, ent.Owner);
    }

    private void OnGibContents(Entity<DevourerComponent> ent, ref BeingGibbedEvent args)
    {
        if (ent.Comp.StomachStorageWhitelist == null)
            return;

        // For some reason we have two different systems that should handle gibbing,
        // and for some another reason GibbingSystem, which should empty all containers, doesn't get involved in this process
        _containerSystem.EmptyContainer(ent.Comp.Stomach);
    }
}

public sealed partial class DevourActionEvent : EntityTargetActionEvent;

[Serializable, NetSerializable]
public sealed partial class DevourDoAfterEvent : SimpleDoAfterEvent;

