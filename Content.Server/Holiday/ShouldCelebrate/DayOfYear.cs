using System;
using Content.Server.Holiday.Interfaces;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Server.Holiday.ShouldCelebrate
{
    /// <summary>
    ///     For a holiday that occurs on a certain day of the year.
    /// </summary>
    [UsedImplicitly]
    public class DayOfYear : IHolidayShouldCelebrate
    {
        private uint _dayOfYear;

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _dayOfYear, "dayOfYear", 1u);
        }

        public bool ShouldCelebrate(DateTime date, HolidayPrototype holiday)
        {
            return date.DayOfYear == _dayOfYear;
        }
    }
}
