/* New Frontiers - Hurgen Changes - modifying the .yml to not be specific to Oni.
This code is licensed under AGPLv3. See AGPLv3.txt */
namespace Content.Server.Abilities.Ovarbi
{
    [RegisterComponent]
    public sealed partial class HeldByOvarbiComponent : Component
    {
        public EntityUid Holder = default!;

        // Frontier: wield accuracy fix
        public double minAngleAdded = 0.0;
        public double maxAngleAdded = 0.0;
        public double angleIncreaseAdded = 0.0;
        // End Frontier
    }
}
// End of modified code
