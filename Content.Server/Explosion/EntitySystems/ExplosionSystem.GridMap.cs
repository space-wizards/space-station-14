namespace Content.Server.Explosion.EntitySystems;

// This partial part of the explosion system has all of the functions used to facilitate explosions moving across grids.
// A good portion of it is focused around keeping track of what tile-indices on a grid correspond to tiles that border
// space. AFAIK no other system currently needs to track these "edge-tiles". If they do, this should probably be a
// property of the grid itself?
public sealed partial class ExplosionSystem : EntitySystem
{
    private void OnGridRemoved(GridRemovalEvent ev)
    {
        _airtightMap.Remove(ev.EntityUid);
    }

}
