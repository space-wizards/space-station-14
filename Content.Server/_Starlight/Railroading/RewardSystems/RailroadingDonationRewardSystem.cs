using Content.Shared._Starlight.Railroading;
using Content.Shared._Starlight.Railroading.Events;
using Content.Shared.Starlight.Economy;
using Robust.Server.Player;
using Robust.Shared.Random;

namespace Content.Server._Starlight.Railroading;

public sealed partial class RailroadingDonationRewardSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SalarySystem _salarySystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RailroadDonationRewardComponent, RailroadingCardCompletedEvent>(OnCompleted);
    }

    private void OnCompleted(Entity<RailroadDonationRewardComponent> ent, ref RailroadingCardCompletedEvent args)
    {
        if (!_playerManager.TryGetSessionByEntity(args.Subject, out var session))
            return;

        var credits = ent.Comp.Amount.Next(_random);
        if (credits == 0)
            return;

        _salarySystem.Donate(session, credits);
    }
}
