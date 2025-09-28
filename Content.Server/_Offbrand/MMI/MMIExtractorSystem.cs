using Content.Server.Body.Components;
using Content.Server.Chat.Systems;
using Content.Server.EUI;
using Content.Shared._Offbrand.MMI;
using Content.Shared._Offbrand.Wounds;
using Content.Shared.Body.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Player;

namespace Content.Server._Offbrand.MMI;

public sealed class MMIExtractorSystem : EntitySystem
{
    [Dependency] private readonly BrainDamageSystem _brainDamage = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MMIExtractorComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<BrainComponent, MMIExtractorDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(Entity<MMIExtractorComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (TryExtract(ent, args.Target.Value, args.User))
            args.Handled = true;
    }

    private bool TryExtract(Entity<MMIExtractorComponent> ent, EntityUid target, EntityUid user)
    {
        if (!_whitelist.CheckBoth(target, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return false;

        if (!_mind.TryGetMind(target, out _, out var mind) || !_player.TryGetSessionById(mind.UserId, out var playerSession))
        {
            _chat.TrySendInGameICMessage(ent,
                Loc.GetString(ent.Comp.NoMind),
                InGameICChatType.Speak,
                true);

            return true;
        }

        if (!_body.TryGetBodyOrganEntityComps<BrainComponent>(target, out var organs))
        {
            _chat.TrySendInGameICMessage(ent,
                Loc.GetString(ent.Comp.Brainless),
                InGameICChatType.Speak,
                true);

            return true;
        }

        if (organs.Count != 1)
        {
            _chat.TrySendInGameICMessage(ent,
                Loc.GetString(ent.Comp.TooManyBrains),
                InGameICChatType.Speak,
                true);

            return true;
        }

        var brain = organs[0];

        _chat.TrySendInGameICMessage(ent,
            Loc.GetString(ent.Comp.Asking),
            InGameICChatType.Speak,
            true);

        var args =
            new DoAfterArgs(EntityManager, user, ent.Comp.Delay, new MMIExtractorDoAfterEvent(), brain, target: target, used: ent)
            {
                NeedHand = true,
                BreakOnMove = true,
                BreakOnWeightlessMove = false,
            };

        if (_doAfter.TryStartDoAfter(args, out var id))
            _eui.OpenEui(new MMIExtractorEui(this, id.Value), playerSession);

        return true;
    }

    public void Decline(DoAfterId id)
    {
        _doAfter.Cancel(id);

        if (!TryComp<DoAfterComponent>(id.Uid, out var doAfters))
            return;

        var dict = doAfters.DoAfters; // i love access workarounds
        if (!dict.TryGetValue(id.Index, out var doAfter))
            return;

        if (doAfter.Args.Used is not { } mmi || !TryComp<MMIExtractorComponent>(mmi, out var mmiComp))
            return;

        _chat.TrySendInGameICMessage(mmi,
            Loc.GetString(mmiComp.Denied),
            InGameICChatType.Speak,
            true);
    }

    public void Accept(DoAfterId id)
    {
        if (!TryComp<DoAfterComponent>(id.Uid, out var doAfters))
            return;

        var dict = doAfters.DoAfters; // i love access workarounds
        if (!dict.TryGetValue(id.Index, out var doAfter))
            return;

        if (doAfter.Args.Used is not { } mmi || !TryComp<MMIExtractorComponent>(mmi, out var mmiComp))
            return;

        if (doAfter.Args.Event is not MMIExtractorDoAfterEvent evt)
            return;

        evt.Accepted = true;
        _chat.TrySendInGameICMessage(mmi,
            Loc.GetString(mmiComp.Accepted),
            InGameICChatType.Speak,
            true);
    }

    private void OnDoAfter(Entity<BrainComponent> ent, ref MMIExtractorDoAfterEvent evt)
    {
        if (evt.Handled || evt.Cancelled)
            return;

        if (evt.Args.Used is not { } mmi || !TryComp<MMIExtractorComponent>(mmi, out var mmiComp))
            return;

        if (!TryComp<MMIComponent>(mmi, out var insertionComp))
            return;

        if (!evt.Accepted)
        {
            _chat.TrySendInGameICMessage(mmi,
                Loc.GetString(mmiComp.NoResponse),
                InGameICChatType.Speak,
                true);

            return;
        }

        if (!_slots.TryInsert(mmi, insertionComp.BrainSlotId, ent, null))
            return;

        if (evt.Args.Target is { } target)
            _brainDamage.KillBrain(target);
    }
}
