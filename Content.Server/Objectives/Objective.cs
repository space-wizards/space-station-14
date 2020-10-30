using Content.Server.Mobs;
using Content.Server.Objectives.Interfaces;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.Objectives
{
    public class Objective
    {
        [ViewVariables]
        public readonly ObjectivePrototype Prototype;

        [ViewVariables]
        public readonly Mind Mind;
        public Objective(Mind mind, ObjectivePrototype prototype)
        {
            Mind = mind;
            Prototype = prototype;
        }
    }
}
