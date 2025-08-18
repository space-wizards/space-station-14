using Content.Shared.Silicons.Laws.Components;
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
    [Dependency] private readonly ItemSlotsSystem _slot = default!;
    private readonly ProtoId<ChatNotificationPrototype> _overrideLawsChatNotificationPrototype = "OverrideLaws";
    private void InitializeOverrider()
    {
        SubscribeLocalEvent<SiliconLawProviderComponent, AfterInteractEvent>(OnOverriderInteract);
        SubscribeLocalEvent<SiliconLawProviderComponent, OverriderDoAfterEvent>(OnOverriderDoAfter);
    }

    private void OnOverriderInteract(Entity<SiliconLawProviderComponent> ent, ref AfterInteractEvent args)
    {
        Log.Debug($"ent: {ent}, args.Target: {args.Target}, args.User: {args.User}, args,.Used: {args.Used}");

        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (!TryComp(args.Target, out SiliconLawProviderComponent? LawProviderTarget))
            return;

        var lawOverrider = args.Used;

        if (!TryComp(lawOverrider, out SiliconLawOverriderComponent? OverriderComp))
            return;

        var lawBoard = _slot.GetItemOrNull(lawOverrider, OverriderComp.LawBoardId);

        if (!TryComp(lawBoard, out SiliconLawOverriderComponent? LawProviderBase))
            return;

        var ev = new ChatNotificationEvent(_overrideLawsChatNotificationPrototype, args.Used, args.User);
        RaiseLocalEvent(ent, ref ev);

        var doAfterTime = OverriderComp.OverrideTime;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, doAfterTime, new OverriderDoAfterEvent(), args.Target, ent.Owner)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
            BreakOnDropItem = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnOverriderDoAfter(Entity<SiliconLawProviderComponent> ent, ref OverriderDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Handled)
            return;

        if (!TryComp(args.Args.Target, out SiliconLawProviderComponent? LawProviderTarget))
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
