
namespace Content.Server.StationEvents.Events;

public sealed class BluespaceArtifact : StationEventSystem
{
    public override string Prototype => "BluespaceArtifact";

    public readonly string ArtifactSpawnerPrototype = "RandomArtifactSpawner";
    public readonly string ArtifactFlashPrototype = "EffectFlashBluespace";

    public override void Added()
    {
        base.Added();

        var str = Loc.GetString("station-event-bluespace-artifact-announcement");
        ChatSystem.DispatchGlobalAnnouncement(str, colorOverride: Color.FromHex("#18abf5"));
    }

    public override void Started()
    {
        base.Started();

        if (!TryFindRandomTile(out _, out _, out _, out var coords))
            return;

        EntityManager.SpawnEntity(ArtifactSpawnerPrototype, coords);
        EntityManager.SpawnEntity(ArtifactFlashPrototype, coords);

        Sawmill.Info($"Spawning random artifact at {coords}");
    }
}
