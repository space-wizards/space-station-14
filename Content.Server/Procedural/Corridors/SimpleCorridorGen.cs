namespace Content.Server.Procedural.Corridors;

public sealed class SimpleCorridorGen : ICorridorGen
{
    public HashSet<Vector2i> CreateCorridor(Vector2i currentRoomCenter, Vector2i destination)
    {
        var corridor = new HashSet<Vector2i>();
        var position = currentRoomCenter;
        corridor.Add(position);

        while (position.Y != destination.Y)
        {
            if (destination.Y > position.Y)
            {
                position += Vector2i.Up;
            }
            else if (destination.Y < position.Y)
            {
                position += Vector2i.Down;
            }
            corridor.Add(position);
        }

        while (position.X != destination.X)
        {
            if (destination.X > position.X)
            {
                position += Vector2i.Right;
            }
            else if(destination.X < position.X)
            {
                position += Vector2i.Left;
            }
            corridor.Add(position);
        }
        return corridor;
    }
}
