using System;
using System.Collections.Immutable;
using JetBrains.Annotations;

namespace Content.Server.GameTicking.Presets
{
    /// <summary>
    ///     Attribute that marks a game preset.
    ///     The id and aliases are registered in lowercase in <see cref="GameTicker"/>.
    ///     A duplicate id or alias will throw an exception.
    /// </summary>
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
