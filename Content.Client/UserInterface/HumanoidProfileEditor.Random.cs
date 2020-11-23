using System;
using System.Linq;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Appearance;
using Content.Shared.Text;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.Client.UserInterface
{
    public partial class HumanoidProfileEditor
    {
        private readonly IRobustRandom _random;

        private void RandomizeEverything()
        {
            Profile = HumanoidCharacterProfile.Random();
            UpdateSexControls();
            UpdateAgeEdit();
            UpdateNameEdit();
            UpdateHairPickers();
        }

        private void RandomizeName()
        {
            var firstName = _random.Pick(Profile.Sex == Sex.Male
                ? Names.MaleFirstNames
                : Names.FemaleFirstNames);
            var lastName = _random.Pick(Names.LastNames);
            SetName($"{firstName} {lastName}");
            UpdateNameEdit();
        }
    }
}
