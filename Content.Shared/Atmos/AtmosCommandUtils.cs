namespace Content.Shared.Atmos
{
    public sealed class AtmosCommandUtils
    {
        /// <summary>
        /// Gas ID parser for atmospherics commands.
        /// This is so there's a central place for this logic for if the Gas enum gets removed.
        /// </summary>
        public static bool TryParseGasID(string str, out int x)
        {
            x = -1;
            if (Enum.TryParse<Gas>(str, true, out var gas))
            {
                x = (int) gas;
            }
            else
            {
                if (!int.TryParse(str, out x))
                    return false;
            }
            return ((x >= 0) && (x < Atmospherics.TotalNumberOfGases));
        }
    }
}
