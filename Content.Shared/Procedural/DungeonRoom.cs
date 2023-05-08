namespace Content.Shared.Procedural;

public sealed record DungeonRoom(HashSet<Vector2i> Tiles, Vector2 Center);
