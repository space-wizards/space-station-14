using Content.Server.Botany.Components;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Botany.Systems;

public sealed class SeedExtractorSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly BotanySystem _botanySystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SeedExtractorComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(EntityUid uid, SeedExtractorComponent component, InteractUsingEvent args)
    {
        if (!TryComp<ApcPowerReceiverComponent>(uid, out var powerReceiverComponent) || !powerReceiverComponent.Powered)
            return;

        if (TryComp(args.Used, out ProduceComponent? produce))
        {
            if (!_prototypeManager.TryIndex<SeedPrototype>(produce.SeedName, out var seed))
                return;

            _popupSystem.PopupCursor(Loc.GetString("seed-extractor-component-interact-message",("name", args.Used)),
                Filter.Entities(args.User));

            QueueDel(args.Used);

            var random = _random.Next(component.MinSeeds, component.MaxSeeds);
            var coords = Transform(uid).Coordinates;

            for (var i = 0; i < random; i++)
            {
                _botanySystem.SpawnSeedPacket(seed, coords);
            }
        }
    }
}
