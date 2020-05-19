using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences
{
    [Serializable, NetSerializable]
    public class HumanoidCharacterProfile : ICharacterProfile
    {
        private readonly Dictionary<string, JobPriority> _jobPriorities;
        public static int MinimumAge = 18;
        public static int MaximumAge = 90;

        private HumanoidCharacterProfile(
            string name,
            int age,
            Sex sex,
            HumanoidCharacterAppearance appearance,
            Dictionary<string, JobPriority> jobPriorities,
            PreferenceUnavailableMode preferenceUnavailable)
        {
            Name = name;
            Age = age;
            Sex = sex;
            Appearance = appearance;
            _jobPriorities = jobPriorities;
            PreferenceUnavailable = preferenceUnavailable;
        }

        public HumanoidCharacterProfile(
            string name,
            int age,
            Sex sex,
            HumanoidCharacterAppearance appearance,
            IReadOnlyDictionary<string, JobPriority> jobPriorities,
            PreferenceUnavailableMode preferenceUnavailable)
            : this(name, age, sex, appearance, new Dictionary<string, JobPriority>(jobPriorities),
                preferenceUnavailable)
        {
        }

        public static HumanoidCharacterProfile Default()
        {
            return new HumanoidCharacterProfile("John Doe", 18, Sex.Male, HumanoidCharacterAppearance.Default(),
                new Dictionary<string, JobPriority>
                {
                    {SharedGameTicker.OverflowJob, JobPriority.High}
                }, PreferenceUnavailableMode.StayInLobby);
        }

        public string Name { get; }
        public int Age { get; }
        public Sex Sex { get; }
        public ICharacterAppearance CharacterAppearance => Appearance;
        public HumanoidCharacterAppearance Appearance { get; }
        public IReadOnlyDictionary<string, JobPriority> JobPriorities => _jobPriorities;
        public PreferenceUnavailableMode PreferenceUnavailable { get; }

        public HumanoidCharacterProfile WithName(string name)
        {
            return new HumanoidCharacterProfile(name, Age, Sex, Appearance, _jobPriorities, PreferenceUnavailable);
        }

        public HumanoidCharacterProfile WithAge(int age)
        {
            return new HumanoidCharacterProfile(Name, age, Sex, Appearance, _jobPriorities, PreferenceUnavailable);
        }

        public HumanoidCharacterProfile WithSex(Sex sex)
        {
            return new HumanoidCharacterProfile(Name, Age, sex, Appearance, _jobPriorities, PreferenceUnavailable);
        }

        public HumanoidCharacterProfile WithCharacterAppearance(HumanoidCharacterAppearance appearance)
        {
            return new HumanoidCharacterProfile(Name, Age, Sex, appearance, _jobPriorities, PreferenceUnavailable);
        }

        public HumanoidCharacterProfile WithJobPriorities(IReadOnlyDictionary<string, JobPriority> jobPriorities)
        {
            return new HumanoidCharacterProfile(
                Name,
                Age,
                Sex,
                Appearance,
                new Dictionary<string, JobPriority>(jobPriorities),
                PreferenceUnavailable);
        }

        public HumanoidCharacterProfile WithJobPriority(string jobId, JobPriority priority)
        {
            var dictionary = new Dictionary<string, JobPriority>(_jobPriorities);
            if (priority == JobPriority.Never)
            {
                dictionary.Remove(jobId);
            }
            else
            {
                dictionary[jobId] = priority;
            }

            return new HumanoidCharacterProfile(Name, Age, Sex, Appearance, dictionary, PreferenceUnavailable);
        }

        public HumanoidCharacterProfile WithPreferenceUnavailable(PreferenceUnavailableMode mode)
        {
            return new HumanoidCharacterProfile(Name, Age, Sex, Appearance, _jobPriorities, mode);
        }

        public string Summary =>
            $"{Name}, {Age} years old {Sex.ToString().ToLower()} human.";

        public bool MemberwiseEquals(ICharacterProfile maybeOther)
        {
            if (!(maybeOther is HumanoidCharacterProfile other)) return false;
            if (Name != other.Name) return false;
            if (Age != other.Age) return false;
            if (Sex != other.Sex) return false;
            if (PreferenceUnavailable != other.PreferenceUnavailable) return false;
            if (!_jobPriorities.SequenceEqual(other._jobPriorities)) return false;
            return Appearance.MemberwiseEquals(other.Appearance);
        }
    }
}
