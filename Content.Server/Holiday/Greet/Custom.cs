using Content.Server.Holiday.Interfaces;
using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Holiday.Greet
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class Custom : IHolidayGreet
    {
        [DataField("text")] private string _greet = string.Empty;

        public string Greet(HolidayPrototype holiday)
        {
            return _greet;
        }
    }
}
