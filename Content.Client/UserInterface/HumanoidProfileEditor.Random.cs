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
            RandomizeSex();
            RandomizeAge();
            RandomizeName();
            RandomizeAppearance();
        }

        private void RandomizeSex()
        {
            SetSex(_random.Prob(0.5f) ? Sex.Male : Sex.Female);
            UpdateSexControls();
        }

        private void RandomizeAge()
        {
            SetAge(_random.Next(HumanoidCharacterProfile.MinimumAge, HumanoidCharacterProfile.MaximumAge));
            UpdateAgeEdit();
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

        private void RandomizeAppearance()
        {
            var newHairStyle = _random.Pick(HairStyles.HairStylesMap.Keys.ToList());
            var newAppearance = Profile.Appearance.WithHairStyleName(newHairStyle);

            var newFacialHairStyle = Profile.Sex == Sex.Female
                ? HairStyles.DefaultFacialHairStyle
                : _random.Pick(HairStyles.FacialHairStylesMap.Keys.ToList());
            newAppearance = newAppearance.WithFacialHairStyleName(newFacialHairStyle);

            var newHairColor = new Color(
                _random.NextFloat(),
                _random.NextFloat(),
                _random.NextFloat());
            newAppearance = newAppearance.WithHairColor(newHairColor);
            newAppearance = newAppearance.WithFacialHairColor(newHairColor);

            Profile = Profile.WithCharacterAppearance(newAppearance);
            UpdateHairPickers();
        }
    }
}
