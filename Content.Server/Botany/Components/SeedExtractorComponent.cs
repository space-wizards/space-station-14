using System.Threading.Tasks;
using Content.Server.Power.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Random;

namespace Content.Server.Botany.Components
{
    [RegisterComponent]
    public class SeedExtractorComponent : Component, IInteractUsing
    {
        [ComponentDependency] private readonly ApcPowerReceiverComponent? _powerReceiver = default!;

        [Dependency] private readonly IRobustRandom _random = default!;

        public override string Name => "SeedExtractor";

        // TODO: Upgradeable machines.
        private int _minSeeds = 1;
        private int _maxSeeds = 4;

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!_powerReceiver?.Powered ?? false)
                return false;

            if (eventArgs.Using.TryGetComponent(out ProduceComponent? produce) && produce.Seed != null)
            {
                eventArgs.User.PopupMessageCursor(Loc.GetString("seed-extractor-component-interact-message",("name", eventArgs.Using.Name)));

                eventArgs.Using.QueueDelete();

                var random = _random.Next(_minSeeds, _maxSeeds);

                for (var i = 0; i < random; i++)
                {
                    produce.Seed.SpawnSeedPacket(Owner.Transform.Coordinates, Owner.EntityManager);
                }

                return true;
            }

            return false;
        }
    }
}
