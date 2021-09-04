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
        [Dependency]
        private readonly IRobustRandom _random = default!;
        public override string Name => "SpecimenDietComponent";

        //The diet is defined by a tag fed into it
        [ViewVariables] [DataField("Diet")] public string[] DietPick { get; set; } = default!;

        public string SelectedDiet = default!;

        public int GrowthState = 0; //0 for embryo, 5 for mature specimen. Anything in-between is embryo state.
        protected override void Initialize()
        {
            IoCManager.Resolve<IRobustRandom>();
            base.Initialize();
            SelectDiet();
        }

        public void SelectDiet()
        {
            IoCManager.Resolve<IRobustRandom>();
            //Picks a random diet, if can pick one
            if (DietPick is not null)
            { 
               SelectedDiet = _random.Pick(DietPick);
            }
        }
    }
}
