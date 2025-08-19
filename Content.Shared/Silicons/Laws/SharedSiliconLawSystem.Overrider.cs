using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Laws;

public abstract partial class SharedSiliconLawSystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedStationAiSystem _stationAi = default!;
    [Dependency] private readonly ItemSlotsSystem _slot = default!;
    private readonly ProtoId<ChatNotificationPrototype> _overrideLawsChatNotificationPrototype = "OverrideLaws";
    private void InitializeOverrider()
    {
        SubscribeLocalEvent<SiliconLawOverriderComponent, AfterInteractEvent>(OnOverriderInteract);
        SubscribeLocalEvent<SiliconLawProviderComponent, OverriderDoAfterEvent>(OnOverriderDoAfter);
    }

    private void OnOverriderInteract(Entity<SiliconLawOverriderComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        EntityUid? Ai = null;

        if (!TryComp(args.Target, out SiliconLawProviderComponent? LawProviderTarget))
        {
            /* if the object doesn't have SiliconLawProviderComponent
            it checks to see if it is the AI core and then trys
            to get the SiliconLawProviderComponent from the AI*/

            if (!TryComp(args.Target, out StationAiCoreComponent? aiCoreComp))
                return;

            _stationAi.TryGetHeld((args.Target.Value, aiCoreComp), out var tempAi);
            Ai = tempAi;

            if (Ai == null)
                return;

            if (!TryComp(Ai, out SiliconLawProviderComponent? LawProviderAi))
                return;

            LawProviderTarget = LawProviderAi;
        }

        var lawOverrider = args.Used;

        if (!TryComp(lawOverrider, out SiliconLawOverriderComponent? OverriderComp))
            return;

        var lawBoard = _slot.GetItemOrNull(lawOverrider, OverriderComp.LawBoardId);

        if (!TryComp(lawBoard, out SiliconLawProviderComponent? LawProviderBase))
            return;

        if (Ai == null)
        {
            var ev = new ChatNotificationEvent(_overrideLawsChatNotificationPrototype, args.Used, args.User);
            RaiseLocalEvent(args.Target.Value, ref ev);
        }
        else
        {
            var ev = new ChatNotificationEvent(_overrideLawsChatNotificationPrototype, args.Used, args.User);
            RaiseLocalEvent(Ai.Value, ref ev);
        }

        var doAfterTime = OverriderComp.OverrideTime;
        DoAfterArgs? doAfterArgs = null;

        if (Ai == null)
        {
            doAfterArgs = new DoAfterArgs(EntityManager, args.User, doAfterTime, new OverriderDoAfterEvent(), args.Target, ent.Owner, args.Used)
            {
                BreakOnDamage = true,
                BreakOnMove = true,
                NeedHand = true,
                BreakOnDropItem = true
            };
        }
        else
        {
            doAfterArgs = new DoAfterArgs(EntityManager, args.User, doAfterTime, new OverriderDoAfterEvent(), Ai.Value, ent.Owner, args.Used)
            {
                BreakOnDamage = true,
                BreakOnMove = true,
                NeedHand = true,
                BreakOnDropItem = true
            };
        }

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnOverriderDoAfter(Entity<SiliconLawProviderComponent> ent, ref OverriderDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Handled)
            return;

        if (!TryComp(ent, out SiliconLawProviderComponent? LawProviderTarget))
            return;

        var lawOverrider = args.Args.Used;

        if (lawOverrider == null)
            return;

        if (!TryComp(lawOverrider.Value, out SiliconLawOverriderComponent? OverriderComp))
            return;

        var lawBoard = _slot.GetItemOrNull(lawOverrider.Value, OverriderComp.LawBoardId);

        if (lawBoard == null)
            return;

        if (!TryComp(lawBoard.Value, out SiliconLawProviderComponent? LawProviderBase))
            return;

        var lawset = GetLawset(LawProviderBase.Laws).Laws;
        SetLaws(lawset, ent, LawProviderBase.LawUploadSound);
    }
}

[Serializable, NetSerializable]
public sealed partial class OverriderDoAfterEvent : SimpleDoAfterEvent;
