using Robust.Shared.Timing;
using Content.Shared.DeviceLinking.Components;
using Content.Shared.Examine;

namespace Content.Shared.DeviceLinking.Systems;

/// <summary>
/// Shared system for RNG device functionality
/// </summary>
public abstract class SharedRngDeviceSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RngDeviceComponent, RngDeviceToggleMuteMessage>(OnToggleMute);
        SubscribeLocalEvent<RngDeviceComponent, RngDeviceToggleEdgeModeMessage>(OnToggleEdgeMode);
        SubscribeLocalEvent<RngDeviceComponent, RngDeviceSetTargetNumberMessage>(OnSetTargetNumber);
        SubscribeLocalEvent<RngDeviceComponent, ExaminedEvent>(OnExamine);
    }

    private void OnToggleMute(Entity<RngDeviceComponent> ent, ref RngDeviceToggleMuteMessage args)
    {
        ent.Comp.Muted = args.Muted;
        Dirty(ent);
    }

    private void OnToggleEdgeMode(Entity<RngDeviceComponent> ent, ref RngDeviceToggleEdgeModeMessage args)
    {
        ent.Comp.EdgeMode = args.EdgeMode;
        Dirty(ent);
    }

    private void OnSetTargetNumber(Entity<RngDeviceComponent> ent, ref RngDeviceSetTargetNumberMessage args)
    {
        if (ent.Comp.Outputs != 2)
            return;

        ent.Comp.TargetNumber = Math.Clamp(args.TargetNumber, 1, 100);
        Dirty(ent);
    }

    private void OnExamine(Entity<RngDeviceComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var comp = ent.Comp;
        args.PushMarkup(Loc.GetString("rng-device-examine-last-roll", ("roll", comp.LastRoll)));

        if (comp.Outputs == 2)  // Only show port info for percentile die
            args.PushMarkup(Loc.GetString("rng-device-examine-last-port", ("port", comp.LastOutputPort)));
    }

    /// <summary>
    /// Generates a random roll and determines the output port based on the number of outputs and target number.
    /// </summary>
    /// <param name="outputs">Number of possible outputs</param>
    /// <param name="targetNumber">Target number for percentile dice (1-100)</param>
    /// <returns>A tuple containing the roll value and the output port</returns>
    protected (int roll, int outputPort) GenerateRoll(int outputs, int targetNumber = 50)
    {
        // Use current tick as seed for deterministic randomness
        var rand = new System.Random((int)Timing.CurTick.Value);

        int roll;
        int outputPort;

        if (outputs == 2)
        {
            // For percentile dice, roll 1-100
            roll = rand.Next(1, 101);
            outputPort = roll <= targetNumber ? 1 : 2;
        }
        else
        {
            roll = rand.Next(1, outputs + 1);
            outputPort = roll;
        }

        return (roll, outputPort);
    }
}
