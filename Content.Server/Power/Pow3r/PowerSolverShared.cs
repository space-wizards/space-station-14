namespace Content.Server.Power.Pow3r
{
    public static class PowerSolverShared
    {
        public static void UpdateRampPositions(float frameTime, PowerState state)
        {
            // Update supplies to move their ramp position towards target, if necessary.
            foreach (var supply in state.Supplies.Values)
            {
                if (supply.Paused)
                    continue;

                if (!supply.Enabled)
                {
                    // If disabled, set ramp to 0.
                    supply.SupplyRampPosition = 0;
                    continue;
                }

                var rampDev = supply.SupplyRampTarget - supply.SupplyRampPosition;
                if (Math.Abs(rampDev) > 0.001f)
                {
                    float newPos;
                    if (rampDev > 0)
                    {
                        // Position below target, go up.
                        newPos = Math.Min(
                            supply.SupplyRampTarget,
                            supply.SupplyRampPosition + supply.SupplyRampRate * frameTime);
                    }
                    else
                    {
                        // Other way around, go down
                        newPos = Math.Max(
                            supply.SupplyRampTarget,
                            supply.SupplyRampPosition - supply.SupplyRampRate * frameTime);
                    }

                    supply.SupplyRampPosition = Math.Clamp(newPos, 0, supply.MaxSupply);
                }
                else
                {
                    supply.SupplyRampPosition = supply.SupplyRampTarget;
                }
            }

            // Batteries too.
            foreach (var battery in state.Batteries.Values)
            {
                if (battery.Paused)
                    continue;

                if (!battery.Enabled)
                {
                    // If disabled, set ramp to 0.
                    battery.SupplyRampPosition = 0;
                    continue;
                }

                var rampDev = battery.SupplyRampTarget - battery.SupplyRampPosition;
                if (Math.Abs(rampDev) > 0.001f)
                {
                    float newPos;
                    if (rampDev > 0)
                    {
                        // Position below target, go up.
                        newPos = Math.Min(
                            battery.SupplyRampTarget,
                            battery.SupplyRampPosition + battery.SupplyRampRate * frameTime);
                    }
                    else
                    {
                        // Other way around, go down
                        newPos = Math.Max(
                            battery.SupplyRampTarget,
                            battery.SupplyRampPosition - battery.SupplyRampRate * frameTime);
                    }

                    battery.SupplyRampPosition = Math.Clamp(newPos, 0, battery.MaxSupply);
                }
                else
                {
                    battery.SupplyRampPosition = battery.SupplyRampTarget;
                }
            }
        }
    }
}
