using Content.Server.CombatMode;
using Content.Server.Interaction;
using Content.Server.Power.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Acts;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Verbs;
using Content.Shared.Xenobiology;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;
using System.Numerics;
using YamlDotNet.Core.Tokens;


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

        private int GrowthState = 0; //0 for embryo, 5 for mature specimen. Anything in-between is embryo state.
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

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            //Newline + base hungry string and empty space + Colored SelectedDiet string picked before
            message.AddText("\n"); 
            message.AddMarkup(Loc.GetString("specimen-growth-hungry") + (" "));
            message.AddText($"[color=#f6ff05]SelectedDiet[/color]");
        }











    }
}
