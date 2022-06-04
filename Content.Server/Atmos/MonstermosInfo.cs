using Content.Shared.Atmos;

namespace Content.Server.Atmos
{
    public struct MonstermosInfo
    {
        [ViewVariables]
        public int LastCycle;

        [ViewVariables]
        public long LastQueueCycle;

        [ViewVariables]
        public long LastSlowQueueCycle;

        [ViewVariables]
        public float MoleDelta;

        [ViewVariables]
        public float TransferDirectionEast;

        [ViewVariables]
        public float TransferDirectionWest;

        [ViewVariables]
        public float TransferDirectionNorth;

        [ViewVariables]
        public float TransferDirectionSouth;

        [ViewVariables]
        public float CurrentTransferAmount;

        [ViewVariables]
        public AtmosDirection CurrentTransferDirection;

        [ViewVariables]
        public bool FastDone;

        public float this[AtmosDirection direction]
        {
            get =>
                direction switch
                {
                    AtmosDirection.East => TransferDirectionEast,
                    AtmosDirection.West => TransferDirectionWest,
                    AtmosDirection.North => TransferDirectionNorth,
                    AtmosDirection.South => TransferDirectionSouth,
                    _ => throw new ArgumentOutOfRangeException(nameof(direction))
                };

            set
            {
                switch (direction)
                {
                    case AtmosDirection.East:
                         TransferDirectionEast = value;
                         break;
                    case AtmosDirection.West:
                        TransferDirectionWest = value;
                        break;
                    case AtmosDirection.North:
                        TransferDirectionNorth = value;
                        break;
                    case AtmosDirection.South:
                        TransferDirectionSouth = value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(direction));
                }
            }
        }

        public float this[int index]
        {
            get => this[(AtmosDirection) (1 << index)];
            set => this[(AtmosDirection) (1 << index)] = value;
        }
    }
}
