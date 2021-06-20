using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Atmos.Reactions;
using Content.Shared.Atmos;
using Robust.Shared.Maths;

namespace Content.Server.Atmos.EntitySystems
{
    public partial class AtmosphereSystem
    {
        private GasReactionPrototype[] _gasReactions = Array.Empty<GasReactionPrototype>();
        private float[] _gasSpecificHeats = new float[Atmospherics.TotalNumberOfGases];

        /// <summary>
        ///     List of gas reactions ordered by priority.
        /// </summary>
        public IEnumerable<GasReactionPrototype> GasReactions => _gasReactions!;
        public float[] GasSpecificHeats => _gasSpecificHeats;

        private void InitializeGases()
        {
            _gasReactions = _protoMan.EnumeratePrototypes<GasReactionPrototype>().ToArray();
            Array.Sort(_gasReactions, (a, b) => b.Priority.CompareTo(a.Priority));

            Array.Resize(ref _gasSpecificHeats, MathHelper.NextMultipleOf(Atmospherics.TotalNumberOfGases, 4));

            for (var i = 0; i < GasPrototypes.Length; i++)
            {
                _gasSpecificHeats[i] = GasPrototypes[i].SpecificHeat;
            }
        }
    }
}
