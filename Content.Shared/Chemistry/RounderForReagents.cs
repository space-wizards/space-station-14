using Content.Shared.Interfaces.Chemistry;
using System;

namespace Content.Shared.Chemistry
{

    public class RounderForReagents : IRounderForReagents
    {
        public decimal Round(decimal value)
        {
            return Math.Round(value, 2);
        }
    }
}
