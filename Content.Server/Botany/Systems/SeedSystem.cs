using Content.Server.Botany.Components;
using Content.Shared.Examine;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Server.Botany.Systems;

public class SeedSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SeedComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, SeedComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!_prototypeManager.TryIndex<Seed>(component.SeedName, out var seed))
            return;

        args.PushMarkup(Loc.GetString($"seed-component-description", ("seedName", seed.DisplayName)));

        if (!seed.RoundStart)
        {
            args.PushMarkup(Loc.GetString($"seed-component-has-variety-tag", ("seedUid", seed.Uid)));
        }
        else
        {
            args.PushMarkup(Loc.GetString($"seed-component-plant-yield-text", ("seedYield", seed.Yield)));
            args.PushMarkup(Loc.GetString($"seed-component-plant-potency-text", ("seedPotency", seed.Potency)));
        }
    }
}
