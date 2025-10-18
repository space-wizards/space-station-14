using Content.Shared.Holiday.Greet;
using Content.Shared.Holiday.Interfaces;
using Content.Shared.Holiday.ShouldCelebrate;
using Robust.Shared.Prototypes;

namespace Content.Shared.Holiday
{
    /// <summary>
    ///     Prototype for holidays. Includes when it occurs, how to greet the server when it occurs,
    ///     and any arbitrary code to run when it occurs.
    /// </summary>
    [Prototype]
    public sealed class HolidayPrototype : IPrototype
    {
        [DataField]
        public LocId Name { get; private set; } = string.Empty;

        /// <inheritdoc />
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        /// <summary>
        ///     The day of the month this holiday begins.
        /// </summary>
        [DataField]
        public byte BeginDay { get; set; } = 1;

        /// <summary>
        ///     The month this holiday begins.
        /// </summary>
        [DataField]
        public Month BeginMonth { get; set; } = Month.Invalid;

        /// <summary>
        ///     Day of the month this holiday will end. Zero means it lasts a single day.
        /// </summary>
        [DataField]
        public byte EndDay { get; set; }

        /// <summary>
        ///     Month this holiday will end in. Invalid means it lasts a single month.
        /// </summary>
        [DataField]
        public Month EndMonth { get; set; } = Month.Invalid;

        /// <summary>
        ///     Logic for how this holiday is celebrated.
        /// </summary>
        [DataField("shouldCelebrate")]
        private IHolidayShouldCelebrate _shouldCelebrate = new DefaultHolidayShouldCelebrate();

        /// <summary>
        ///     What to announce to the server when the round starts.
        /// </summary>
        [DataField("greet")]
        private IHolidayGreet _greet = new DefaultHolidayGreet();

        /// <summary>
        ///     Arbitrary code to run when the round starts.
        /// </summary>
        [DataField("celebrate")]
        private IHolidayCelebrate? _celebrate;

        public bool ShouldCelebrate(DateTime date)
        {
            return _shouldCelebrate.ShouldCelebrate(date, this);
        }

        public string Greet()
        {
            return _greet.Greet(this);
        }

        /// <summary>
        ///     Called before the round starts to set up any festive shenanigans.
        /// </summary>
        public void Celebrate()
        {
            _celebrate?.Celebrate(this);
        }
    }
}
