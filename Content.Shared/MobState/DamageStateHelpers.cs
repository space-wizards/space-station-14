namespace Content.Shared.MobState
{
    public static class DamageStateHelpers
    {
        /// <summary>
        ///     Enumerates over <see cref="DamageState"/>, returning them in order
        ///     of alive to dead.
        /// </summary>
        /// <returns>An enumerable of <see cref="DamageState"/>.</returns>
        public static IEnumerable<DamageState> AliveToDead()
        {
            foreach (DamageState state in Enum.GetValues(typeof(DamageState)))
            {
                if (state == DamageState.Invalid)
                {
                    continue;
                }

                yield return state;
            }
        }
    }
}
