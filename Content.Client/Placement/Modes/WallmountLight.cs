using System.Numerics;
using Robust.Client.Placement;
using Robust.Shared.Map;

namespace Content.Client.Placement.Modes
{
    public sealed class WallmountLight : PlacementMode
    {
        public WallmountLight(PlacementManager pMan) : base(pMan)
        {
        }

        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            MouseCoords = ScreenToCursorGrid(mouseScreen);
            CurrentTile = GetTileRef(MouseCoords);

            if (pManager.CurrentPermission!.IsTile)
            {
                return;
            }

            var tileCoordinates = new EntityCoordinates(MouseCoords.EntityId, CurrentTile.GridIndices);

            Vector2 offset;
            switch (pManager.Direction)
            {
                case Direction.North:
                    offset = new Vector2(0.5f, 1f);
                    break;
                case Direction.South:
                    offset = new Vector2(0.5f, 0f);
                    break;
                case Direction.East:
                    offset = new Vector2(1f, 0.5f);
                    break;
                case Direction.West:
                    offset = new Vector2(0f, 0.5f);
                    break;
                default:
                    return;
            }

            tileCoordinates = tileCoordinates.Offset(offset);
            MouseCoords = tileCoordinates;
        }

        public override bool IsValidPosition(EntityCoordinates position)
        {
            if (pManager.CurrentPermission!.IsTile)
            {
                return false;
            }
            else if (!RangeCheck(position))
            {
                return false;
            }

            return true;
        }
    }
}
