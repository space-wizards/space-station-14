using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.Containers;

namespace Content.Shared.Silicons.Laws;

public abstract partial class SharedSiliconLawSystem
{
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

        if (!TryComp(args.Target, out SiliconLawProviderComponent? targetOverrider))
            return;

        if (!TryComp(args.Used, out SiliconLawOverriderComponent? OverriderComp))
            return;

        var doAfterTime = OverriderComp.OverrideTime;

        if (TryGetHeld((args.Target.Value, targetOverrider), out var held))
        {
            var ev = new ChatNotificationEvent(_downloadChatNotificationPrototype, args.Used, args.User);
            RaiseLocalEvent(held, ref ev);
        }

        // TODO: RETRIVE LAW PROVIDER FROM LAW BOARD IN THE ITEM SLOT

        // TODO: PASS THE LAW PROVIDER TO THE DOAFTER EVENT

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

    protected virtual void OnOverriderDoAfter(Entity<SiliconLawProviderComponent> ent, ref OverriderDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Handled)
            return;

        if (!TryComp(args.Args.Target, out SiliconLawProviderComponent? targetOverrider))
            return;

        var provider; //TODO: GET THE LAW PROVIDER HERE SOMEHOW

        var lawset = GetLawset(provider.Laws).Laws;
        var query = EntityManager.CompRegistryQueryEnumerator(ent.Comp.Components);

        while (query.MoveNext(out var update))
        {
            SetLaws(lawset, update, provider.LawUploadSound);
        }
    }
}

[Serializable, NetSerializable]
public sealed partial class OverriderDoAfterEvent : SimpleDoAfterEvent;
