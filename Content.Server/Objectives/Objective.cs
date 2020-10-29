using Content.Server.Mobs;
using Content.Server.Objectives.Interfaces;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.Objectives
{
    public class Objective
    {
        public readonly ObjectivePrototype Prototype;

        public readonly Mind Mind;
        public Objective(Mind mind, ObjectivePrototype prototype)
        {
            Mind = mind;
            Prototype = prototype;
        }
    }
}
