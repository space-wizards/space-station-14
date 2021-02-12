using System;
using System.Collections.Immutable;
using JetBrains.Annotations;

namespace Content.Server.GameTicking.GamePresets
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [BaseTypeRequired(typeof(GamePreset))]
    [MeansImplicitUse]
    public class GamePresetAttribute : Attribute
    {
        public string Id { get; }

        public ImmutableList<string> Aliases { get; }

        public GamePresetAttribute(string id, params string[] aliases)
        {
            Id = id;
            Aliases = aliases.ToImmutableList();
        }
    }
}
