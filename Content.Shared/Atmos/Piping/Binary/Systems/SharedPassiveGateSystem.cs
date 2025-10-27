using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Examine;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Atmos.Piping.Binary.Systems;

public abstract class SharedPassiveGateSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasPassiveGateComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<GasPassiveGateComponent> gate, ref ExaminedEvent args)
    {
        if (!Comp<TransformComponent>(gate).Anchored || !args.IsInDetailsRange) // Not anchored? Out of range? No status.
            return;

        var str = _netManager.IsServer
            ? Loc.GetString("gas-passive-gate-examined", ("flowRate", $"{gate.Comp.FlowRate:0.#}"))
            : Loc.GetString("gas-passive-gate-updating");

        args.PushMarkup(str);
    }
}
