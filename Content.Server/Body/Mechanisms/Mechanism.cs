#nullable enable
using System;
using System.Collections.Generic;
using Content.Server.Body.Mechanisms.Behaviors;
using Content.Server.GameObjects.Components.Body;
using Content.Server.GameObjects.Components.Metabolism;
using Content.Shared.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body.Mechanisms
{
    /// <summary>
    ///     Data class representing a persistent item inside a <see cref="IBodyPart"/>.
    ///     This includes livers, eyes, cameras, brains, explosive implants,
    ///     binary communicators, and other things.
    /// </summary>
    public class Mechanism
    {
        private IBodyPart? _part;

        public Mechanism(MechanismPrototype data)
        {
            Data = data;
            Id = null!;
            Name = null!;
            Description = null!;
            ExamineMessage = null!;
            RSIPath = null!;
            RSIState = null!;
            Behaviors = new List<MechanismBehavior>();
        }

        [ViewVariables] private bool Initialized { get; set; }

        [ViewVariables] private MechanismPrototype Data { get; set; }

        [ViewVariables] public string Id { get; private set; }

        [ViewVariables] public string Name { get; set; }

        /// <summary>
        ///     Professional description of the <see cref="Mechanism"/>.
        /// </summary>
        [ViewVariables]
        public string Description { get; set; }

        /// <summary>
        ///     The message to display upon examining a mob with this Mechanism installed.
        ///     If the string is empty (""), no message will be displayed.
        /// </summary>
        [ViewVariables]
        public string ExamineMessage { get; set; }

        /// <summary>
        ///     Path to the RSI that represents this <see cref="Mechanism"/>.
        /// </summary>
        [ViewVariables]
        public string RSIPath { get; set; }

        /// <summary>
        ///     RSI state that represents this <see cref="Mechanism"/>.
        /// </summary>
        [ViewVariables]
        public string RSIState { get; set; }

        /// <summary>
        ///     Max HP of this <see cref="Mechanism"/>.
        /// </summary>
        [ViewVariables]
        public int MaxDurability { get; set; }

        /// <summary>
        ///     Current HP of this <see cref="Mechanism"/>.
        /// </summary>
        [ViewVariables]
        public int CurrentDurability { get; set; }

        /// <summary>
        ///     At what HP this <see cref="Mechanism"/> is completely destroyed.
        /// </summary>
        [ViewVariables]
        public int DestroyThreshold { get; set; }

        /// <summary>
        ///     Armor of this <see cref="Mechanism"/> against attacks.
        /// </summary>
        [ViewVariables]
        public int Resistance { get; set; }

        /// <summary>
        ///     Determines a handful of things - mostly whether this
        ///     <see cref="Mechanism"/> can fit into a <see cref="IBodyPart"/>.
        /// </summary>
        [ViewVariables]
        public int Size { get; set; }

        /// <summary>
        ///     What kind of <see cref="IBodyPart"/> this <see cref="Mechanism"/> can be
        ///     easily installed into.
        /// </summary>
        [ViewVariables]
        public BodyPartCompatibility Compatibility { get; set; }

        /// <summary>
        ///     The behaviors that this <see cref="Mechanism"/> performs.
        /// </summary>
        [ViewVariables]
        private List<MechanismBehavior> Behaviors { get; }

        public IBodyManagerComponent? Body => Part?.Body;

        public IBodyPart? Part
        {
            get => _part;
            set
            {
                var old = _part;
                _part = value;

                if (value == null && old != null)
                {
                    foreach (var behavior in Behaviors)
                    {
                        behavior.RemovedFromPart(old);
                    }
                }
                else
                {
                    foreach (var behavior in Behaviors)
                    {
                        behavior.InstalledIntoPart();
                    }
                }
            }
        }

        public void EnsureInitialize()
        {
            if (Initialized)
            {
                return;
            }

            LoadFromPrototype(Data);
            Initialized = true;
        }

        /// <summary>
        ///     Loads the given <see cref="MechanismPrototype"/>.
        ///     Current data on this <see cref="Mechanism"/> will be overwritten!
        /// </summary>
        private void LoadFromPrototype(MechanismPrototype data)
        {
            Data = data;
            Id = data.ID;
            Name = data.Name;
            Description = data.Description;
            ExamineMessage = data.ExamineMessage;
            RSIPath = data.RSIPath;
            RSIState = data.RSIState;
            MaxDurability = data.Durability;
            CurrentDurability = MaxDurability;
            DestroyThreshold = data.DestroyThreshold;
            Resistance = data.Resistance;
            Size = data.Size;
            Compatibility = data.Compatibility;

            foreach (var behavior in Behaviors.ToArray())
            {
                RemoveBehavior(behavior);
            }

            foreach (var mechanismBehaviorName in data.BehaviorClasses)
            {
                var mechanismBehaviorType = Type.GetType(mechanismBehaviorName);

                if (mechanismBehaviorType == null)
                {
                    throw new InvalidOperationException(
                        $"No {nameof(MechanismBehavior)} found with name {mechanismBehaviorName}");
                }

                if (!mechanismBehaviorType.IsSubclassOf(typeof(MechanismBehavior)))
                {
                    throw new InvalidOperationException(
                        $"Class {mechanismBehaviorName} is not a subtype of {nameof(MechanismBehavior)} for mechanism prototype {data.ID}");
                }

                var newBehavior = IoCManager.Resolve<IDynamicTypeFactory>().CreateInstance<MechanismBehavior>(mechanismBehaviorType);

                AddBehavior(newBehavior);
            }
        }

        public void InstalledIntoBody()
        {
            foreach (var behavior in Behaviors)
            {
                behavior.InstalledIntoBody();
            }
        }

        public void RemovedFromBody(IBodyManagerComponent old)
        {
            foreach (var behavior in Behaviors)
            {
                behavior.RemovedFromBody(old);
            }
        }

        /// <summary>
        ///     This method is called by <see cref="IBodyPart.PreMetabolism"/> before
        ///     <see cref="MetabolismComponent.Update"/> is called.
        /// </summary>
        public void PreMetabolism(float frameTime)
        {
            foreach (var behavior in Behaviors)
            {
                behavior.PreMetabolism(frameTime);
            }
        }

        /// <summary>
        ///     This method is called by <see cref="IBodyPart.PostMetabolism"/> after
        ///     <see cref="MetabolismComponent.Update"/> is called.
        /// </summary>
        public void PostMetabolism(float frameTime)
        {
            foreach (var behavior in Behaviors)
            {
                behavior.PostMetabolism(frameTime);
            }
        }

        private void AddBehavior(MechanismBehavior behavior)
        {
            Behaviors.Add(behavior);
            behavior.Initialize(this);
        }

        private bool RemoveBehavior(MechanismBehavior behavior)
        {
            behavior.Remove();
            return Behaviors.Remove(behavior);
        }
    }
}
