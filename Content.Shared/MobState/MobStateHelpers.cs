namespace Content.Shared.MobState
{
    public static class MobStateHelpers
    {
        /// <summary>
        ///     Enumerates over <see cref="MobState"/>, returning them in order
        ///     of alive to dead.
        /// </summary>
        /// <returns>An enumerable of <see cref="MobState"/>.</returns>
        public static IEnumerable<MobState> AliveToDead()
        {
            foreach (MobState state in Enum.GetValues(typeof(MobState)))
            {
                if (state == MobState.Invalid)
                {
                    continue;
                }

                yield return state;
            }
        }
    }
}
