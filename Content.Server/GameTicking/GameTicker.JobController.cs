using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Station.Components;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking
{
    // This code is responsible for the assigning & picking of jobs.
    public sealed partial class GameTicker
    {
        [Conditional("DEBUG")]
        private void InitializeJobController()
        {
            // Verify that the overflow role exists and has the correct name.
            var role = _prototypeManager.Index<JobPrototype>(FallbackOverflowJob);
            DebugTools.Assert(role.Name == Loc.GetString(FallbackOverflowJobName),
                "Overflow role does not have the correct name!");
        }
    }
}
