using Content.Shared.Coordinates.Helpers;
using Content.Shared.Directions;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.Starlight.Energy.Supermatter;

public sealed class SupermatterCascadeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    private readonly LinkedList<Branch> _branches = [];
    private LinkedListNode<Branch>? node;
    private readonly string[] _prototypes = ["Cascad1", "Cascad2", "Cascad3", "Cascad4", "Cascad5", "Cascad6"];
    public override void Initialize()
    {
    }

    public override void Update(float frameTime)
    {
        node ??= _branches.First;
        if (node == null) return;
        var branch = node.Value;
        var nextNode = node.Next;

        branch.Lifetime--;
        if (branch.Lifetime <= 0)
        {
            _branches.Remove(node);
            node = nextNode;
            return;
        }

        var rand = _random.Next(0, 100);

        if (rand < 10)
        {
            branch.Direction = branch.RotateLeft();
        }
        else if (rand < 20)
        {
            branch.Direction = branch.RotateRight();
        }
        else if (rand < 25 && _branches.Count < 10)
        {
            var leftBranch = new Branch
            {
                Coordinates = branch.Coordinates,
                Direction = branch.RotateLeft(),
                Lifetime = branch.Lifetime
            };
            var rightBranch = new Branch
            {
                Coordinates = branch.Coordinates,
                Direction = branch.RotateRight(),
                Lifetime = branch.Lifetime
            };

            _branches.AddLast(leftBranch);
            _branches.AddLast(rightBranch);

            _branches.Remove(node);
            node = nextNode;
            return;
        }

        branch.Coordinates = branch.Coordinates.Offset(branch.Direction);
        if (_transform.GetGrid(branch.Coordinates) is not { } grid
            || !TryComp<MapGridComponent>(grid, out var gridComp)
            || !_map.TryGetTileRef(grid, gridComp, branch.Coordinates, out var tileRef)
            || _turf.IsSpace(tileRef))
        {
            _branches.Remove(node);
            node = nextNode;
            return;
        }
        branch.Coordinates = branch.Coordinates.SnapToGrid(gridComp);

        foreach (var entity in _lookup.GetEntitiesIntersecting(branch.Coordinates))
            QueueDel(entity);

        SpawnAttachedTo(_random.Pick(_prototypes), branch.Coordinates);

        node = nextNode;
    }

    public void StartCascade(EntityCoordinates coordinates)
    {
        for (int i = 0; i < 8; i += 2)
            _branches.AddLast(new Branch
            {
                Coordinates = coordinates,
                Direction = (Direction)i,
                Lifetime = 100
            });
    }

    private sealed class Branch
    {
        public EntityCoordinates Coordinates { get; set; }
        public Direction Direction { get; set; }
        public int Lifetime { get; set; }

        public Direction RotateLeft() => Direction switch
        {
            Direction.North => Direction.NorthWest,
            Direction.NorthWest => Direction.West,
            Direction.West => Direction.SouthWest,
            Direction.SouthWest => Direction.South,
            Direction.South => Direction.SouthEast,
            Direction.SouthEast => Direction.East,
            Direction.East => Direction.NorthEast,
            Direction.NorthEast => Direction.North,
            _ => Direction,
        };

        public Direction RotateRight() => Direction switch
        {
            Direction.North => Direction.NorthEast,
            Direction.NorthEast => Direction.East,
            Direction.East => Direction.SouthEast,
            Direction.SouthEast => Direction.South,
            Direction.South => Direction.SouthWest,
            Direction.SouthWest => Direction.West,
            Direction.West => Direction.NorthWest,
            Direction.NorthWest => Direction.North,
            _ => Direction,
        };
    }
}
