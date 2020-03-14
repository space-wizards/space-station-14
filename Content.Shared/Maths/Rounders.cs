using System;

namespace Content.Shared.Maths
{
    public static class Rounders
    {
        public static decimal RoundForReagents(this decimal me)
        {
            return Math.Round(me, 2);
        }
    }
}
