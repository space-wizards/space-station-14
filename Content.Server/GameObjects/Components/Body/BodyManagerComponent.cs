#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Body;
using Content.Server.Body.Network;
using Content.Server.GameObjects.Components.Metabolism;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Observer;
using Content.Shared.Body.Part;
using Content.Shared.Body.Preset;
using Content.Shared.Body.Template;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Movement;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Body
{
    /// <summary>
    ///     Component representing a collection of <see cref="IBodyPart"></see>
    ///     attached to each other.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IDamageableComponent))]
    [ComponentReference(typeof(ISharedBodyManagerComponent))]
    [ComponentReference(typeof(IBodyPartManager))]
    [ComponentReference(typeof(IBodyManagerComponent))]
    public partial class BodyManagerComponent : SharedBodyManagerComponent, IBodyPartContainer, IRelayMoveInput, IBodyManagerComponent
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IBodyNetworkFactory _bodyNetworkFactory = default!;
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;

        [ViewVariables] private string _presetName = default!;

        [ViewVariables] private readonly Dictionary<Type, BodyNetwork> _networks = new Dictionary<Type, BodyNetwork>();

        [ViewVariables] public BodyTemplate Template { get; private set; } = default!;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction(
                "baseTemplate",
                "bodyTemplate.Humanoid",
                template =>
                {
                    if (!_prototypeManager.TryIndex(template, out BodyTemplatePrototype prototype))
                    {
                        // Invalid prototype
                        throw new InvalidOperationException(
                            $"No {nameof(BodyTemplatePrototype)} found with name {template}");
                    }

                    Template = new BodyTemplate();
                    Template.Initialize(prototype);
                },
                () => Template.Name);

            serializer.DataReadWriteFunction(
                "basePreset",
                "bodyPreset.BasicHuman",
                preset =>
                {
                    if (!_prototypeManager.TryIndex(preset, out BodyPresetPrototype prototype))
                    {
                        // Invalid prototype
                        throw new InvalidOperationException(
                            $"No {nameof(BodyPresetPrototype)} found with name {preset}");
                    }

                    Preset = new BodyPreset();
                    Preset.Initialize(prototype);
                },
                () => _presetName);
        }

        public override void Initialize()
        {
            base.Initialize();

            LoadBodyPreset(Preset);
        }

        protected override void Startup()
        {
            base.Startup();

            // Just in case something activates at default health.
            ForceHealthChangedEvent();
        }

        private void LoadBodyPreset(BodyPreset preset)
        {
            _presetName = preset.Name;

            foreach (var slotName in Template.Slots.Keys)
            {
                // For each slot in our BodyManagerComponent's template,
                // try and grab what the ID of what the preset says should be inside it.
                if (!preset.PartIDs.TryGetValue(slotName, out var partId))
                {
                    // If the preset doesn't define anything for it, continue.
                    continue;
                }

                // Get the BodyPartPrototype corresponding to the BodyPart ID we grabbed.
                if (!_prototypeManager.TryIndex(partId, out BodyPartPrototype newPartData))
                {
                    throw new InvalidOperationException($"No {nameof(BodyPartPrototype)} prototype found with ID {partId}");
                }

                // Try and remove an existing limb if that exists.
                RemovePart(slotName, false);

                // Add a new BodyPart with the BodyPartPrototype as a baseline to our
                // BodyComponent.
                var addedPart = new BodyPart(newPartData);
                TryAddPart(slotName, addedPart);
            }

            OnBodyChanged(); // TODO: Duplicate code
        }

        // /// <summary>
        // ///     Changes the current <see cref="BodyTemplate"/> to the given
        // ///     <see cref="BodyTemplate"/>.
        // ///     Attempts to keep previous <see cref="IBodyPart"/> if there is a
        // ///     slot for them in both <see cref="BodyTemplate"/>.
        // /// </summary>
        // public void ChangeBodyTemplate(BodyTemplatePrototype newTemplate)
        // {
        //     foreach (var part in Parts)
        //     {
        //         // TODO: Make this work.
        //     }
        //
        //     OnBodyChanged();
        // }

        /// <summary>
        ///     This method is called by <see cref="BodySystem.Update"/> before
        ///     <see cref="MetabolismComponent.Update"/> is called.
        /// </summary>
        public void PreMetabolism(float frameTime)
        {
            if (CurrentDamageState == DamageState.Dead)
            {
                return;
            }

            foreach (var part in Parts.Values)
            {
                part.PreMetabolism(frameTime);
            }

            foreach (var network in _networks.Values)
            {
                network.PreMetabolism(frameTime);
            }
        }

        /// <summary>
        ///     This method is called by <see cref="BodySystem.Update"/> after
        ///     <see cref="MetabolismComponent.Update"/> is called.
        /// </summary>
        public void PostMetabolism(float frameTime)
        {
            if (CurrentDamageState == DamageState.Dead)
            {
                return;
            }

            foreach (var part in Parts.Values)
            {
                part.PostMetabolism(frameTime);
            }

            foreach (var network in _networks.Values)
            {
                network.PostMetabolism(frameTime);
            }
        }

        void IRelayMoveInput.MoveInputPressed(ICommonSession session)
        {
            if (CurrentDamageState == DamageState.Dead)
            {
                new Ghost().Execute(null, (IPlayerSession) session, null);
            }
        }

        #region BodyNetwork Functions

        private bool EnsureNetwork(BodyNetwork network)
        {
            DebugTools.AssertNotNull(network);

            if (_networks.ContainsKey(network.GetType()))
            {
                return true;
            }

            _networks.Add(network.GetType(), network);
            network.OnAdd(Owner);

            return false;
        }

        /// <summary>
        ///     Attempts to add a <see cref="BodyNetwork"/> of the given type to this body.
        /// </summary>
        /// <returns>
        ///     True if successful, false if there was an error
        ///     (such as passing in an invalid type or a network of that type already
        ///     existing).
        /// </returns>
        public bool EnsureNetwork(Type networkType)
        {
            DebugTools.Assert(networkType.IsSubclassOf(typeof(BodyNetwork)));

            var network = _bodyNetworkFactory.GetNetwork(networkType);
            return EnsureNetwork(network);
        }

        /// <summary>
        ///     Attempts to add a <see cref="BodyNetwork"/> of the given type to
        ///     this body.
        /// </summary>
        /// <typeparam name="T">The type of network to add.</typeparam>
        /// <returns>
        ///     True if successful, false if there was an error
        ///     (such as passing in an invalid type or a network of that type already
        ///     existing).
        /// </returns>
        public bool EnsureNetwork<T>() where T : BodyNetwork
        {
            return EnsureNetwork(typeof(T));
        }

        public void RemoveNetwork(Type networkType)
        {
            DebugTools.AssertNotNull(networkType);

            if (_networks.Remove(networkType, out var network))
            {
                network.OnRemove();
            }
        }

        public void RemoveNetwork<T>() where T : BodyNetwork
        {
            RemoveNetwork(typeof(T));
        }

        /// <summary>
        ///     Attempts to get the <see cref="BodyNetwork"/> of the given type in this body.
        /// </summary>
        /// <param name="networkType">The type to search for.</param>
        /// <param name="result">
        ///     The <see cref="BodyNetwork"/> if found, null otherwise.
        /// </param>
        /// <returns>True if found, false otherwise.</returns>
        public bool TryGetNetwork(Type networkType, [NotNullWhen(true)] out BodyNetwork result)
        {
            return _networks.TryGetValue(networkType, out result!);
        }

        #endregion
    }

    public interface IBodyManagerHealthChangeParams
    {
        BodyPartType Part { get; }
    }

    public class BodyManagerHealthChangeParams : HealthChangeParams, IBodyManagerHealthChangeParams
    {
        public BodyManagerHealthChangeParams(BodyPartType part)
        {
            Part = part;
        }

        public BodyPartType Part { get; }
    }
}
