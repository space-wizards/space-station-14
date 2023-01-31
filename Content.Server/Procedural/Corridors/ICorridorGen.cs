namespace Content.Server.Procedural.Corridors;

public interface ICorridorGen
{
    HashSet<Vector2i> CreateCorridor(Vector2i currentRoomCenter, Vector2i destination);
}
