#nullable enable
using System;
using System.Collections.Generic;
using Content.Server.Body.Mechanisms.Behaviors;
using Content.Server.GameObjects.Components.Body;
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
    public class Mechanism : IMechanism
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
            _behaviors = new List<MechanismBehavior>();
        }

        [ViewVariables] private bool Initialized { get; set; }

        [ViewVariables] private MechanismPrototype Data { get; set; }

        [ViewVariables] public string Id { get; private set; }

        [ViewVariables] public string Name { get; set; }

        [ViewVariables] public string Description { get; set; }

        [ViewVariables] public string ExamineMessage { get; set; }

        [ViewVariables] public string RSIPath { get; set; }

        [ViewVariables] public string RSIState { get; set; }

        [ViewVariables] public int MaxDurability { get; set; }

        [ViewVariables] public int CurrentDurability { get; set; }

        [ViewVariables] public int DestroyThreshold { get; set; }

        [ViewVariables] public int Resistance { get; set; }

        [ViewVariables] public int Size { get; set; }

        [ViewVariables] public BodyPartCompatibility Compatibility { get; set; }

        private readonly List<MechanismBehavior> _behaviors;

        [ViewVariables] public IReadOnlyList<MechanismBehavior> Behaviors => _behaviors;

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

            foreach (var behavior in _behaviors.ToArray())
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

        public void PreMetabolism(float frameTime)
        {
            foreach (var behavior in Behaviors)
            {
                behavior.PreMetabolism(frameTime);
            }
        }

        public void PostMetabolism(float frameTime)
        {
            foreach (var behavior in Behaviors)
            {
                behavior.PostMetabolism(frameTime);
            }
        }

        public void AddBehavior(MechanismBehavior behavior)
        {
            _behaviors.Add(behavior);
            behavior.Initialize(this);
        }

        public bool RemoveBehavior(MechanismBehavior behavior)
        {
            if (_behaviors.Remove(behavior))
            {
                behavior.Remove();
                return true;
            }

            return false;
        }
    }
}
