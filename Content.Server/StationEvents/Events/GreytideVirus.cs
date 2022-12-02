using System.Linq;
using Content.Server.Doors.Components;
using Content.Server.Doors.Systems;
using Content.Server.GameTicking.Rules.Configurations;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class GreytideVirus : StationEventSystem
{
    [Dependency] private readonly DoorSystem _doorSystem = default!;

    public override string Prototype => "GreytideVirus";

    public override void Started()
    {
        base.Started();

        var modifier = GetSeverityModifier();

        var airlocks = EntityQuery<AirlockComponent, DoorComponent>().ToList();
        RobustRandom.Shuffle(airlocks);

        var airlockAmount = (int) (RobustRandom.Next(2, 4) * Math.Sqrt(modifier));

        for (var i = 0; i < airlockAmount && i < airlocks.Count - 1; i++)
        {
                var airlock = RobustRandom.Pick(airlocks);
                Sawmill.Info($"Bolting and opening {airlock}");

                _doorSystem.TryOpen(airlock.Item1.Owner, airlock.Item2);
                airlock.Item1.SetBoltsWithAudio(true);
        }
    }
}
