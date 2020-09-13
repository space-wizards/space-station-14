#nullable enable
using System;
using Content.Server.Body;
using Content.Server.Observer;
using Content.Shared.Body.Part;
using Content.Shared.Body.Preset;
using Content.Shared.Body.Template;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Movement;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Body
{
    /// <summary>
    ///     Component representing a collection of <see cref="IBodyPart"></see>
    ///     attached to each other.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IDamageableComponent))]
    [ComponentReference(typeof(IBody))]
    [ComponentReference(typeof(IBodyPartManager))]
    public partial class BodyComponent : SharedBodyComponent, IBodyPartContainer, IRelayMoveInput
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;

        [ViewVariables] private string _presetName = default!;

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

        void IRelayMoveInput.MoveInputPressed(ICommonSession session)
        {
            if (CurrentDamageState == DamageState.Dead)
            {
                new Ghost().Execute(null, (IPlayerSession) session, null);
            }
        }
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
