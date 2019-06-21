using System;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;
using Robust.Shared.Interfaces.GameObjects;
using Content.Server.Interfaces.GameObjects.Components.Mobs;
using Robust.Shared.Prototypes;

namespace Content.Server.GameObjects.Components.Mobs.Body
{
    /// <summary>
    ///     Organ handles damage through states, and transfers DATA to <see cref="IBodyFunction"/> systems
    ///     it also decides which entity to spawn on ejection
    /// </summary>
    public class Organ //: IPrototype TODO: when YAML comes, i have to fix "No PrototypeAttribute to give it a type string."
    {
        public string Name;

        public List<OrganNode> Nodes;

        public int Health;

        public OrganState State = OrganState.Healthy;

        public virtual void LoadFrom(YamlMappingNode mapping)
        {

        }

        public Organ(string name, int health, List<OrganNode> nodes, OrganState state)
        {
            Name = name;
            Health = health;
            Nodes = nodes;
            State = state;
        }
    }

    public enum OrganState
    {
        Healthy,
        Boosted,
        Damaged,
        Missing
    }
}