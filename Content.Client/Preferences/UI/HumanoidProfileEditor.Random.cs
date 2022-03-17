using Content.Shared.CharacterAppearance;
using Content.Shared.Dataset;
using Content.Shared.Preferences;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client.Preferences.UI
{
    public sealed partial class HumanoidProfileEditor
    {
        private readonly IRobustRandom _random;
        private readonly IPrototypeManager _prototypeManager;

        private void RandomizeEverything()
        {
            Profile = HumanoidCharacterProfile.Random();
            UpdateSexControls();
            UpdateGenderControls();
            UpdateClothingControls();
            UpdateAgeEdit();
            UpdateNameEdit();
            UpdateHairPickers();
            UpdateEyePickers();

            _skinColor.Value = _random.Next(0, 100);
        }

        private void RandomizeName()
        {
            if (Profile == null) return;
            var firstName = _random.Pick(Profile.Sex.FirstNames(_prototypeManager).Values);
            var lastName = _random.Pick(_prototypeManager.Index<DatasetPrototype>("names_last"));
            SetName($"{firstName} {lastName}");
            UpdateNameEdit();
        }
    }
}
