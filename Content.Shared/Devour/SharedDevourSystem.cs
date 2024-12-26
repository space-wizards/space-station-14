using Content.Shared.Actions;
using Content.Shared.Devour.Components;
using Content.Shared.DoAfter;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared.Devour;

public abstract class SharedDevourSystem : EntitySystem
{
    [Dependency] protected readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] protected readonly SharedContainerSystem ContainerSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DevourerComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<DevourerComponent, DevourActionEvent>(OnDevourAction);
    }

    protected void OnInit(EntityUid uid, DevourerComponent component, MapInitEvent args)
    {
        //Devourer doesn't actually chew, since he sends targets right into his stomach.
        //I did it mom, I added ERP content into upstream. Legally!
        component.Stomach = ContainerSystem.EnsureContainer<Container>(uid, "stomach");

        _actionsSystem.AddAction(uid, ref component.DevourActionEntity, component.DevourAction);
    }

    /// <summary>
    /// The devour action
    /// </summary>
    protected void OnDevourAction(EntityUid uid, DevourerComponent component, DevourActionEvent args)
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
}

public sealed partial class DevourActionEvent : EntityTargetActionEvent { }

[Serializable, NetSerializable]
public sealed partial class DevourDoAfterEvent : SimpleDoAfterEvent { }

[Serializable, NetSerializable]
public enum FoodPreference : byte
{
    Humanoid = 0,
    All = 1
}
