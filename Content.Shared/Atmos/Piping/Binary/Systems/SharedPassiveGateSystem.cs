using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Examine;

namespace Content.Shared.Atmos.Piping.Binary.Systems;

public abstract class SharedPassiveGateSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasPassiveGateComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<GasPassiveGateComponent> gate, ref ExaminedEvent args)
    {
        if (!Comp<TransformComponent>(gate).Anchored || !args.IsInDetailsRange) // Not anchored? Out of range? No status.
            return;

        var str = Loc.GetString("gas-passive-gate-examined", ("flowRate", $"{gate.Comp.FlowRate:0.#}"));
        args.PushMarkup(str);
    }
}
