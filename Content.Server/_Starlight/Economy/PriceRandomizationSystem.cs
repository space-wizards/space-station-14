using Content.Shared.GameTicking;
using Robust.Shared.Configuration;

namespace Content.Server.Economy
{
    public sealed class PriceRandomizationSystem : EntitySystem
    {
        [Dependency] private readonly ItemPriceManager _priceManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        }

        private void OnRoundRestart(RoundRestartCleanupEvent args) => _priceManager.ResetForNewRound();
    }
}