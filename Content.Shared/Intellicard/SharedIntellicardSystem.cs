using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Intellicard;

/// <summary>
/// System for handling the behaviour of intellicards.
/// </summary>
public abstract class SharedIntellicardSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly SharedStationAiSystem _stationAi = default!;

    /// <summary>
    /// Prototype of the StationAiBrain, used for spawning in a new entity.
    /// </summary>
    private readonly EntProtoId _defaultAi = "StationAiBrain";

    /// <summary>
    /// Prototype of the StationAi job, used for adding and removing it from the late join menu.
    /// </summary>
    private readonly EntProtoId _stationAiJob = "StationAi";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IntellicardComponent, EntInsertedIntoContainerMessage>(OnIntellicardInsert);
        SubscribeLocalEvent<IntellicardComponent, EntRemovedFromContainerMessage>(OnIntellicardRemove);

        SubscribeLocalEvent<StationAiConverterComponent, AfterInteractUsingEvent>(OnUsingBorgBrainOnConverterInteract);

        SubscribeLocalEvent<StationAiHolderComponent, BorgBrainInsertIntoHolderDoAfter>(OnBrainToHolderDoAfter);
    }

    private void OnBrainToHolderDoAfter(Entity<StationAiHolderComponent> ent, ref BorgBrainInsertIntoHolderDoAfter args)
    {
        if (args.Cancelled || args.Handled || args.Args.Used == null)
            return;

        if (!TryComp(args.Args.Target, out StationAiHolderComponent? targetHolder))
            return;

        if (_stationAi.TryGetHeldFromHolder((args.Args.Target.Value, targetHolder), out _))
            return;

        if (!_mind.TryGetMind(args.Args.Used.Value, out var mindId, out var mind))
            return;

        // Swap the job of the mind.
        _roles.MindAddJobRole(mindId, mind, false, _stationAiJob);

        // Try to create a new AiBrain inside of the targetHolder
        var brain = SpawnInContainerOrDrop(_defaultAi, ent.Owner, StationAiHolderComponent.Container);
        _mind.TransferTo(mindId, brain);
        RandomizeAiName(brain);
        QueueDel(args.Args.Used);

        _popup.PopupEntity(Loc.GetString("ai-convert-finished"), args.User, args.User, PopupType.Medium);

        if(HasComp<StationAiCoreComponent>(ent.Owner))
            _stationAi.TryRemoveAiJobSlot(ent.Owner);
    }

    private void OnUsingBorgBrainOnConverterInteract(Entity<StationAiConverterComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (!TryComp(args.Target, out StationAiHolderComponent? targetHolder))
            return;

        if (!HasComp<BorgBrainComponent>(args.Used))
            return;

        //We don't want an empty mind to be insertable.
        if (!_mind.TryGetMind(args.Used, out var _, out var _))
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.NoMindPopup), args.User, args.User, PopupType.Medium);
            return;
        }

        if (_stationAi.TryGetHeldFromHolder((args.Target.Value, targetHolder), out var _))
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.OccupiedPopup), args.User, args.User, PopupType.Medium);
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.ConvertTime, new BorgBrainInsertIntoHolderDoAfter(), args.Target, ent.Owner, used: args.Used)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
            BreakOnDropItem = true
        };

        _popup.PopupEntity(Loc.GetString(ent.Comp.WarningPopup), args.User, args.User, PopupType.MediumCaution);
        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnIntellicardInsert(Entity<IntellicardComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_gameTiming.ApplyingState)
            return;

        if (args.Container.ID != IntellicardComponent.Container)
            return;

        // This is how the AI Core gets renamed to the AI's name. Unsure if there is a better way to do this.
        _metadata.SetEntityName(ent.Owner, MetaData(args.Entity).EntityName);
    }

    private void OnIntellicardRemove(Entity<IntellicardComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (_gameTiming.ApplyingState)
            return;

        _metadata.SetEntityName(ent.Owner, Prototype(ent.Owner)?.Name ?? string.Empty);
    }

    protected virtual void RandomizeAiName(EntityUid ent) { }
}

[Serializable, NetSerializable]
public sealed partial class BorgBrainInsertIntoHolderDoAfter : SimpleDoAfterEvent;
