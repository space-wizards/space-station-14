using Content.Server.Holiday.Interfaces;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Server.Holiday.Greet
{
    [UsedImplicitly]
    public class Custom : IHolidayGreet
    {
        private string _greet;

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _greet, "text", string.Empty);
        }

        public string Greet(HolidayPrototype holiday)
        {
            return _greet;
        }
    }
}
