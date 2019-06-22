using System;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;
using Robust.Shared.Interfaces.GameObjects;
using Content.Server.Interfaces.GameObjects.Components.Mobs;
using Robust.Shared.Prototypes;

namespace Content.Server.GameObjects.Components.Mobs.Body
{
    /// <summary>
    ///     Organ acts just like component - it gets called by main Life() function of the body and have to process the data on tick
    /// </summary>

    public abstract class Organ //: IPrototype TODO: when YAML comes, i have to fix "No PrototypeAttribute to give it a type string."
    {
        public string Name;

        public int Health;

        public OrganState State = OrganState.Healthy;

        public OrganStatus Status = OrganStatus.Normal;

        public Dictionary<string, object> OrganData; //TODO

        public virtual void mockInit(string name, int health, OrganState state, OrganStatus status) //Temp code before YAML 
        {
            Name = name;
            Health = health;
            State = state;
            Status = status;
        }

        public virtual void LoadFrom(YamlMappingNode mapping)
        {

        }

        public abstract void Life();
    }

    public enum OrganState
    {
        Healthy,
        Damaged,
        Missing
    }

    public enum OrganStatus
    {
        Normal,
        Bleed,
        Boost,
        Stasis,
        Cancer
    }
}
