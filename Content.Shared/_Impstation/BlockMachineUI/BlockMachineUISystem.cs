using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Content.Shared.Whitelist;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._Impstation.BlockMachineUI;

public sealed class SharedBlockMachineUISystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlockMachineUIComponent, UserOpenActivatableUIAttemptEvent>(OnUIOpenAttempt);
    }

    public void OnUIOpenAttempt(Entity<BlockMachineUIComponent> ent, ref UserOpenActivatableUIAttemptEvent args)
    {
        if (ent.Comp.Whitelist == null && ent.Comp.Blacklist == null)
            args.Cancel();

        if (_whitelist.IsWhitelistPassOrNull(ent.Comp.Whitelist, args.Target) && _whitelist.IsBlacklistFailOrNull(ent.Comp.Blacklist, args.Target))
            return;

        args.Cancel();

        if (_net.IsClient && _timing.IsFirstTimePredicted && ent.Comp.PopupText != null)
            _popup.PopupEntity(Loc.GetString(ent.Comp.PopupText), ent, ent);
    }
}