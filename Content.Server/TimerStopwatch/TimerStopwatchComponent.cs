using Robust.Shared.GameObjects;
using System;

namespace Content.Server.TimerStopwatch
{
    //NOTE: If named "stopwatch" causes conflicts with the engine stopwatch system
    [RegisterComponent]
    public class TimerStopwatchComponent: Component
    {
        public override string Name => "TimerStopwatch";

        /// <summary>
        /// The amount of time passed since the tracking started
        /// </summary>
        public float PassedTime = 0;

        /// <summary>
        /// If the stopwatch is currently tracking the time
        /// </summary>
        public bool TrackingStatus = false;
    }
}
