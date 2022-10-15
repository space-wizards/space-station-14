using System.Collections;
using System.Linq;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Shared.Directions;

public static class SharedDirectionExtensions
{
    public static EntityCoordinates Offset(this EntityCoordinates coordinates, Direction direction)
    {
        return coordinates.Offset(direction.ToVec());
    }
}

public readonly struct DirectionRandomizer : IEnumerable<Direction>
{
    private readonly Direction[]? _directions;

    public static DirectionRandomizer RandomCardinal()
    {
        return new DirectionRandomizer(new[]
        {
            Direction.East,
            Direction.West,
            Direction.South,
            Direction.North,
        });
    }

    public static DirectionRandomizer RandomDirection()
    {
        return new DirectionRandomizer(new[]
        {
            Direction.East,
            Direction.NorthEast,
            Direction.West,
            Direction.NorthWest,
            Direction.South,
            Direction.SouthWest,
            Direction.North,
            Direction.SouthEast,
        });
    }

    public DirectionRandomizer(IEnumerable<Direction> directions)
    {
        _directions = directions.ToArray();
        Count = _directions.Length;
    }

    private DirectionRandomizer(Direction[] directions)
    {
        _directions = directions;
        Count = _directions.Length;
    }

    public void Dispose()
    {
    }

    public int Count { get; }

    public readonly Span<Direction> Span => new(_directions, 0, Count);

    public struct Enumerator : IEnumerator<Direction>
    {
        private readonly DirectionRandomizer _randomizer;
        private int _index;

        internal Enumerator(DirectionRandomizer randomizer)
        {
            _index = -1;
            _randomizer = randomizer;
            if (_randomizer._directions == null)
                return;

            var robustRandom = IoCManager.Resolve<IRobustRandom>();
            var n = _randomizer._directions.Length;

            while (n > 1)
            {
                n--;
                var k = robustRandom.Next(n + 1);
                (_randomizer._directions[k], _randomizer._directions[n]) =
                    (_randomizer._directions[n], _randomizer._directions[k]);
            }
        }

        public bool MoveNext()
        {
            return ++_index < _randomizer.Count;
        }

        public void Reset()
        {
            _index = -1;
        }

        public Direction Current => _randomizer._directions![_index];

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }
    }

    public IEnumerator<Direction> GetEnumerator() => new Enumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);
}
