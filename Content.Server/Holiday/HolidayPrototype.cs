#nullable enable
using System;
using Content.Server.Holiday.Celebrate;
using Content.Server.Holiday.Greet;
using Content.Server.Holiday.Interfaces;
using Content.Server.Holiday.ShouldCelebrate;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Holiday
{
    [Prototype("holiday")]
    public class HolidayPrototype : IPrototype
    {
        [ViewVariables] [DataField("name")] public string Name { get; private set; } = string.Empty;

        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [ViewVariables]
        [DataField("beginDay")]
        public byte BeginDay { get; set; } = 1;

        [ViewVariables]
        [DataField("beginMonth")]
        public Month BeginMonth { get; set; } = Month.Invalid;

        /// <summary>
        ///     Day this holiday will end. Zero means it lasts a single day.
        /// </summary>
        [ViewVariables]
        [DataField("endDay")]
        public byte EndDay { get; set; }

        /// <summary>
        ///     Month this holiday will end in. Invalid means it lasts a single month.
        /// </summary>
        [ViewVariables]
        [DataField("endMonth")]
        public Month EndMonth { get; set; } = Month.Invalid;

        [ViewVariables] [DataField("shouldCelebrate")]
        private readonly IHolidayShouldCelebrate _shouldCelebrate = new DefaultHolidayShouldCelebrate();

        [ViewVariables] [DataField("greet")]
        private readonly IHolidayGreet _greet = new DefaultHolidayGreet();

        [ViewVariables] [DataField("celebrate")]
        private readonly IHolidayCelebrate _celebrate = new DefaultHolidayCelebrate();

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
            _celebrate.Celebrate(this);
        }
    }
}
