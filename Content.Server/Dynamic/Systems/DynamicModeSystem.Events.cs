using System;
using System.Collections.Generic;

namespace Content.Server.Dynamic.Systems;

public partial class DynamicModeSystem
{
    /// <summary>
    ///     Holds the number of times each event has been run.
    /// </summary>
    public Dictionary<string, int> TotalEvents = new();

    /// <summary>
    ///     Holds the active events, with the value representing
    ///     when the event started, for refunding purposes.
    /// </summary>
    public Dictionary<string, TimeSpan> ActiveEvents = new();


}
