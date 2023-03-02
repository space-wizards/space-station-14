

namespace Content.Server.Patron
{
    [RegisterComponent]
    [Access(typeof(PatronSystem))]
    public sealed class PatronEarsComponent : Component
    {
        [DataField("sprite")]
        public string RsiPath = "Clothing/Head/Hats/catears.rsi";
    }
}
