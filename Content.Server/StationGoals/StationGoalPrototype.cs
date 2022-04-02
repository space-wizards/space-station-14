using Robust.Shared.Prototypes;

namespace Content.Server.StationGoals
{
    /// <summary>
    ///     A station goal info.
    /// </summary>
    [Prototype("stationGoal")]
    public sealed class StationGoalPrototype : IPrototype
    {
        [DataField("id", required:true)]
        public string ID { get; } = default!;

        [DataField("text", required:true)]
        public string Text { get; } = default!;
    }
}
