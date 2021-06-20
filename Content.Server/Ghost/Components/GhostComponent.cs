using System;
using Content.Shared.Ghost;
using Robust.Shared.GameObjects;

#nullable enable
namespace Content.Server.Ghost.Components
{
    [RegisterComponent]
    public class GhostComponent : SharedGhostComponent
    {
        public TimeSpan TimeOfDeath { get; set; } = TimeSpan.Zero;
            var deathTimeInfo = timeSinceDeath.Minutes > 0 ? Loc.GetString($"ghost-component-on-examine-death-time-info-minutes", ("minutes", timeSinceDeath.Minutes)) :
                                                             Loc.GetString($"ghost-component-on-examine-death-time-info-seconds", ("seconds", timeSinceDeath.Seconds));
            message.AddMarkup(Loc.GetString("ghost-component-on-examine-message",("timeOfDeath", deathTimeInfo)));
    }
}
