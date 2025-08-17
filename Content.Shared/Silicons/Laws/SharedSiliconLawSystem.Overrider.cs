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

        if (!TryComp(args.Target, out SiliconLawProviderComponent? LawProviderTarget))
            return;

        if (!TryComp(args.Used, out SiliconLawOverriderComponent? OverriderComp))
            return;

        var lawBoard = _slot.GetItemOrNull(ent, OverriderComp.LawBoardId);

        if (!TryComp(lawBoard, out SiliconLawOverriderComponent? LawProviderBase))
            return;

        var ev = new ChatNotificationEvent(_overrideLawsChatNotificationPrototype, args.Used, args.User);
        RaiseLocalEvent(ent, ref ev);

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

    private void OnOverriderDoAfter(Entity<SiliconLawProviderComponent> ent, ref OverriderDoAfterEvent args)
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

        var lawset = GetLawset(LawProviderBase.Laws).Laws;

        SetLaws(lawset, ent, LawProviderBase.LawUploadSound);
    }

    protected virtual void SetLaws(List<SiliconLaw> newLaws, EntityUid target, SoundSpecifier? cue = null)
    {

    }
}

[Serializable, NetSerializable]
public sealed partial class OverriderDoAfterEvent : SimpleDoAfterEvent
{
    public EntityUid? LawProviderBaseEntity;

    public OverriderDoAfterEvent(EntityUid? lawProviderBaseEntity)
    {
        LawProviderBaseEntity = lawProviderBaseEntity;
    }
}
