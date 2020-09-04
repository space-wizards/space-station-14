using System.Collections.Generic;
using Content.Shared.Body.Part;
using Content.Shared.Body.Preset;
using Robust.Shared.Utility;
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
        [ViewVariables] public bool Initialized { get; private set; }

        [ViewVariables] public string Name { get; protected set; }

        /// <summary>
        ///     Maps a template slot to the ID of the <see cref="IBodyPart"/>
        ///     that should fill it. E.g. "right arm" : "BodyPart.arm.basic_human".
        /// </summary>
        [ViewVariables]
        public Dictionary<string, string> PartIDs { get; protected set;  }

        public virtual void Initialize(BodyPresetPrototype prototype)
        {
            DebugTools.Assert(!Initialized, $"{nameof(BodyPreset)} {Name} has already been initialized!");

            Name = prototype.Name;
            PartIDs = prototype.PartIDs;

            Initialized = true;
        }
    }
}
