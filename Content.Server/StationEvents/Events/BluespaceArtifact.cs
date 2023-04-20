using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class BluespaceArtifact : StationEventSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override string Prototype => "BluespaceArtifact";

    public readonly string ArtifactSpawnerPrototype = "RandomArtifactSpawner";
    public readonly string ArtifactFlashPrototype = "EffectFlashBluespace";

    public readonly List<string> PossibleSighting = new()
    {
        "bluespace-artifact-sighting-1",
        "bluespace-artifact-sighting-2",
        "bluespace-artifact-sighting-3",
        "bluespace-artifact-sighting-4",
        "bluespace-artifact-sighting-5",
        "bluespace-artifact-sighting-6",
        "bluespace-artifact-sighting-7"
    };

    public override void Added()
    {
        base.Added();

        var str = Loc.GetString("bluespace-artifact-event-announcement",
            ("sighting", Loc.GetString(_random.Pick(PossibleSighting))));
        ChatSystem.DispatchGlobalAnnouncement(str, colorOverride: Color.FromHex("#18abf5"));
    }

    public override void Started()
    {
        base.Started();
        var amountToSpawn = Math.Max(1, (int) MathF.Round(GetSeverityModifier() / 1.5f));
        for (var i = 0; i < amountToSpawn; i++)
        {
            if (!TryFindRandomTile(out _, out _, out _, out var coords))
                return;

            EntityManager.SpawnEntity(ArtifactSpawnerPrototype, coords);
            EntityManager.SpawnEntity(ArtifactFlashPrototype, coords);

            Sawmill.Info($"Spawning random artifact at {coords}");
        }
    }
}
