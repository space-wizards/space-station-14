using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;


namespace Content.Server.Xenobiology
{
    [RegisterComponent]
    public class SpecimenDietComponent : Component
    {

        public override string Name => "SpecimenDiet";

        //The diet is defined by a tag fed into it
        [ViewVariables] [DataField("diet")] public string[] DietPick { get; set; } = default!;

        public string SelectedDiet = default!;

        public int GrowthState = 0; //0 for embryo, 5 for mature specimen. Anything in-between is embryo state.
    }
}
