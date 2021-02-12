using System;
using Robust.Shared.Serialization;

namespace Content.Server.Holiday.Interfaces
{
    public interface IHolidayShouldCelebrate : IExposeData
    {
        void IExposeData.ExposeData(ObjectSerializer serializer) {}
        bool ShouldCelebrate(DateTime date, HolidayPrototype holiday);
    }
}
