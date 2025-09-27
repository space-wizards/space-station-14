using Content.Shared.Holiday.Interfaces;
using JetBrains.Annotations;

namespace Content.Shared.Holiday.Greet
{
    /// <summary>
    ///     Custom greeting for displaying any festive text you want!
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class Custom : IHolidayGreet
    {
        [DataField("text", required: true)]
        private LocId _greet;

        public string Greet(HolidayPrototype holiday)
        {
            return Loc.GetString(_greet);
        }
    }
}
