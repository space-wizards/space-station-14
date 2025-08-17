using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Laws;

public abstract partial class SharedSiliconLawSystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
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

        if (!TryComp(args.Target, out SiliconLawProviderComponent? LawProviderTarget))
            return;

        if (!TryComp(args.Used, out SiliconLawOverriderComponent? OverriderComp))
            return;

        var lawBoard = _slot.GetItemOrNull(ent, OverriderComp.LawBoardId);

        if (!TryComp(lawBoard, out SiliconLawOverriderComponent? LawProviderBase))
            return;

        if (TryGetHeld((args.Target.Value, LawProviderTarget), out var held))
            {
                var ev = new ChatNotificationEvent(_downloadChatNotificationPrototype, args.Used, args.User);
                RaiseLocalEvent(held, ref ev);
            }

        // TODO: PASS THE LAW PROVIDER TO THE DOAFTER EVENT

        var doAfterTime = OverriderComp.OverrideTime;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, doAfterTime, new OverriderDoAfterEvent(lawBoard), args.Target, ent.Owner)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
            BreakOnDropItem = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    protected virtual void OnOverriderDoAfter(Entity<SiliconLawProviderComponent> ent, ref OverriderDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Handled)
            return;

        if (!TryComp(args.Args.Target, out SiliconLawProviderComponent? LawProviderTarget))
            return;

        if (args.LawProviderBaseEntity is not { } lawProviderBaseEntity)
            return;

        if (!TryComp(lawProviderBaseEntity, out SiliconLawProviderComponent? LawProviderBase))
            return;

        // TODO: UPDATE LAWS
    }
}

[Serializable, NetSerializable]
public sealed partial class OverriderDoAfterEvent(EntityUid? lawProviderBaseEntity = null) : SimpleDoAfterEvent
{
    public EntityUid? LawProviderBaseEntity = lawProviderBaseEntity;
}
