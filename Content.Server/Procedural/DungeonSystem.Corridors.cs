using Content.Server.Procedural.Corridors;

namespace Content.Server.Procedural;

public sealed partial class DungeonSystem
{
    // Why aren't these on the classes themselves? Coz dependencies.

    public HashSet<Vector2i> CreateCorridor(ICorridorGen gen, Vector2i currentRoomCenter, Vector2i destination)
    {
        switch (gen)
        {
            case SimpleCorridorGen simple:
                return CreateCorridor(simple, currentRoomCenter, destination);
            default:
                throw new NotImplementedException();
        }
    }

    public HashSet<Vector2i> CreateCorridor(SimpleCorridorGen gen, Vector2i currentRoomCenter, Vector2i destination)
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
