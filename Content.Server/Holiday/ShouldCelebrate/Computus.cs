using System.IO;
using JetBrains.Annotations;

namespace Content.Server.Holiday.ShouldCelebrate
{
    /// <summary>
    ///     Computus for easter calculation.
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class Computus : DefaultHolidayShouldCelebrate
    {
        [DataField("daysEarly")]
        private byte _daysEarly = 1;

        [DataField("daysExtra")]
        private byte _daysExtra = 1;

        public (int day, int month) DoComputus(DateTime date)
        {
            var currentYear = date.Year;
            var m = 0;
            var n = 0;

            switch (currentYear)
            {
                case var i when i >= 1900 && i <= 2099:
                    m = 24;
                    n = 5;
                    break;

                case var i when i >= 2100 && i <= 2199:
                    m = 24;
                    n = 6;
                    break;

                case var i when i >= 2200 && i <= 2299:
                    m = 25;
                    n = 0;
                    break;

                // Hello, future person! If you're living in the year >=2300, you might want to fix this method.
                // t. earth coder living in 2021
                default:
                    throw new InvalidDataException("Easter machine broke.");
            }

            var a = currentYear % 19;
            var b = currentYear % 4;
            var c = currentYear % 7;
            var d = (19 * a + m) % 30;
            var e = (2 * b + 4 * c + 6 * d + n) % 7;

            (int day, int month) easterDate = (0, 0);

            if (d + e < 10)
            {
                easterDate.month = 3;
                easterDate.day = (d + e + 22);
            } else if (d + e > 9)
            {
                easterDate.month = 4;
                easterDate.day = (d + e - 9);
            }

            if (easterDate.month == 4 && easterDate.day == 26)
                easterDate.day = 19;

            if (easterDate.month == 4 && easterDate.day == 25 && d == 28 && e == 6 && a > 10)
                easterDate.day = 18;

            return easterDate;
        }

        public override bool ShouldCelebrate(DateTime date, HolidayPrototype holiday)
        {
            if (holiday.BeginMonth == Month.Invalid)
            {
                var (day, month) = DoComputus(date);

                holiday.BeginDay = (byte) day;
                holiday.BeginMonth = (Month) month;

                holiday.EndDay = (byte) (holiday.BeginDay + _daysExtra);
                holiday.EndMonth = holiday.BeginMonth;

                // Begins in march, ends in april
                if (holiday.EndDay >= 32 && holiday.EndMonth == Month.March)
                {
                    holiday.EndDay -= 31;
                    holiday.EndMonth++;
                }

                // Begins in april, ends in june.
                if (holiday.EndDay >= 31 && holiday.EndMonth == Month.April)
                {
                    holiday.EndDay -= 30;
                    holiday.EndMonth++;
                }

                holiday.BeginDay -= _daysEarly;
                // Begins in march, ends in april.
                if (holiday.BeginDay <= 0 && holiday.BeginMonth == Month.April)
                {
                    holiday.BeginDay += 31;
                    holiday.BeginMonth--;
                }
            }

            return base.ShouldCelebrate(date, holiday);
        }
    }
}
