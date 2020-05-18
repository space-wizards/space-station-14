namespace Content.Server.Utility
{
    public static class InteractionHelpers
    {
        // /// <summary>
        // ///     Checks that these coordinates are within a certain distance without any
        // ///     entity that matches the collision mask obstructing them.
        // ///     If the <paramref name="range"/> is zero or negative,
        // ///     this method will only check if nothing obstructs the two sets of coordinates..
        // /// </summary>
        // /// <param name="coords">Set of coordinates to use.</param>
        // /// <param name="otherCoords">Other set of coordinates to use.</param>
        // /// <param name="range">maximum distance between the two sets of coordinates.</param>
        // /// <param name="collisionMask">the mask to check for collisions</param>
        // /// <param name="ignoredEnt">the entity to be ignored when checking for collisions.</param>
        // /// <param name="mapManager">Map manager containing the two GridIds.</param>
        // /// <param name="insideBlockerValid">if coordinates inside obstructions count as obstructed or not</param>
        // /// <returns>True if the two points are within a given range without being obstructed.</returns>
        // public static bool InRangeUnobstructed(MapCoordinates coords, Vector2 otherCoords, float range = InteractionRange,
        //     int collisionMask = (int) CollisionGroup.Impassable, IEntity ignoredEnt = null,
        //     bool insideBlockerValid = false)
        // {
        //     if (!EntitySystemHelpers.EntitySystem<SharedInteractionSystem>().InRangeUnobstructed(player.Transform.MapPosition, Owner.Transform.WorldPosition, ignoredEnt: Owner))
        //     {
        //         _notifyManager.PopupMessage(Owner.Transform.GridPosition, player, _localizationManager.GetString("You can't reach there!"));
        //         return;
        //     }
        // }


    }
}
