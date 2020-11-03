namespace Content.Shared.GameObjects.Components.Damage
{
    public static class DamageableComponentExtensions
    {
        public static int? EarliestIncapacitatedThreshold(this IDamageableComponent damageable)
        {
            foreach (var state in DamageStateHelpers.AliveToDead())
            {
                if (damageable.Thresholds.TryGetValue(state, out var threshold))
                {
                    return threshold;
                }
            }

            if (damageable.Thresholds.TryGetValue(DamageState.Alive, out var aliveThreshold))
            {
                return aliveThreshold;
            }

            return null;
        }

        public static bool TryGetEarliestIncapacitatedThreshold(this IDamageableComponent damageable, out int threshold)
        {
            var tempThreshold = damageable.EarliestIncapacitatedThreshold();

            if (tempThreshold == null)
            {
                threshold = default;
                return false;
            }

            threshold = tempThreshold.Value;
            return true;
        }
    }
}
