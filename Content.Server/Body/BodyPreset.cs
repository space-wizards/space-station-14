using System.Collections.Generic;
using Content.Shared.Body.Part;
using Content.Shared.Body.Preset;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body
{
    /// <summary>
    ///     Stores data on what <see cref="BodyPartPrototype"></see> should
    ///     fill a BodyTemplate.
    ///     Used for loading complete body presets, like a "basic human" with all
    ///     human limbs.
    /// </summary>
    public class BodyPreset
    {
        public BodyPreset(BodyPresetPrototype data)
        {
            LoadFromPrototype(data);
        }

        [ViewVariables] public string Name { get; private set; }

        /// <summary>
        ///     Maps a template slot to the ID of the <see cref="IBodyPart"/> that should
        ///     fill it. E.g. "right arm" : "BodyPart.arm.basic_human".
        /// </summary>
        [ViewVariables]
        public Dictionary<string, string> PartIDs { get; private set; }

        protected virtual void LoadFromPrototype(BodyPresetPrototype data)
        {
            Name = data.Name;
            PartIDs = data.PartIDs;
        }
    }
}
