#nullable enable
using System.Collections.Generic;
using Content.Shared.Damage.DamageContainer;
using Content.Shared.Damage.ResistanceSet;
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body.Surgery;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Body.Part
{
    public abstract class SharedBodyPartComponent : Component, IBodyPart, ICanExamine, IShowContextMenu
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        // TODO define these in yaml?
        public const string DefaultDamageContainer = "biologicalDamageContainer";
        public const string DefaultResistanceSet = "defaultResistances";

        public override string Name => "BodyPart";

        private IBody? _body;

        // TODO Remove
        private List<string> _mechanismIds = new List<string>();

        private DamageContainerPrototype _damagePrototype = default!;
        private ResistanceSetPrototype _resistancePrototype = default!;

        private HashSet<IMechanism> _mechanisms = new HashSet<IMechanism>();

        [ViewVariables]
        public IBody? Body
        {
            get => _body;
            set
            {
                if (_body == value)
                {
                    return;
                }

                var old = _body;
                _body = value;

                if (value != null)
                {
                    foreach (var mechanism in _mechanisms)
                    {
                        mechanism.OnBodyAdd(old, value);
                    }
                }
                else if (old != null)
                {
                    foreach (var mechanism in _mechanisms)
                    {
                        mechanism.OnBodyRemove(old);
                    }
                }
            }
        }

        [ViewVariables] public BodyPartType PartType { get; private set; }

        [ViewVariables] public string Plural { get; private set; } = string.Empty;

        [ViewVariables] public int Size { get; private set; }

        [ViewVariables] public int SizeUsed { get; private set; }

        [ViewVariables] public int MaxDurability { get; private set; }

        // TODO MaxDurability - Damage.TotalDamage;
        // TODO Individual body part damage
        [ViewVariables] public int CurrentDurability { get; private set; }

        // TODO size used
        // TODO surgerydata
        // TODO properties

        /// <summary>
        ///     At what HP this <see cref="IBodyPart"/> destroyed.
        /// </summary>
        [ViewVariables]
        public int DestroyThreshold { get; private set; }

        /// <summary>
        ///     What types of BodyParts this <see cref="IBodyPart"/> can easily attach to.
        ///     For the most part, most limbs aren't universal and require extra work to
        ///     attach between types.
        /// </summary>
        [ViewVariables]
        public BodyPartCompatibility Compatibility { get; private set; }

        /// <summary>
        ///     Set of all <see cref="IMechanism"/> currently inside this
        ///     <see cref="IBodyPart"/>.
        /// </summary>
        [ViewVariables]
        public IReadOnlyCollection<IMechanism> Mechanisms => _mechanisms;

        // TODO Replace with a simulation of organs
        /// <summary>
        ///     Represents if body part is vital for creature.
        ///     If the last vital body part is removed creature dies
        /// </summary>
        [ViewVariables]
        public bool IsVital { get; private set; }

        // TODO
        /// <summary>
        ///     Current damage dealt to this <see cref="IBodyPart"/>.
        /// </summary>
        [ViewVariables]
        public DamageContainer Damage { get; private set; } = default!;

        // TODO
        /// <summary>
        ///     Armor of this <see cref="IBodyPart"/> against damage.
        /// </summary>
        [ViewVariables]
        public ResistanceSet Resistances { get; private set; } = default!;

        // TODO
        [ViewVariables]
        public SurgeryDataComponent? SurgeryDataComponent => Owner.GetComponentOrNull<SurgeryDataComponent>();

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            // TODO BODY Separate damage from the rest of body system
            // TODO serialize any changed properties?
            serializer.DataReadWriteFunction(
                "damagePrototype",
                DefaultDamageContainer,
                prototype =>
                {
                    _damagePrototype = _prototypeManager.Index<DamageContainerPrototype>(prototype);
                    Damage = new DamageContainer(OnHealthChanged, _damagePrototype);
                },
                () => _damagePrototype.ID);

            serializer.DataReadWriteFunction(
                "resistancePrototype",
                DefaultResistanceSet,
                prototype =>
                {
                    _resistancePrototype = _prototypeManager.Index<ResistanceSetPrototype>(prototype);
                    Resistances = new ResistanceSet(_resistancePrototype);
                },
                () => _resistancePrototype.ID);

            serializer.DataField(this, b => b.PartType, "partType", BodyPartType.Other);

            serializer.DataField(this, b => b.Plural, "plural", string.Empty);

            serializer.DataField(this, b => b.Size, "size", 1);

            serializer.DataField(this, b => b.MaxDurability, "maxDurability", 10);

            serializer.DataField(this, b => b.CurrentDurability, "currentDurability", MaxDurability);

            serializer.DataField(this, m => m.IsVital, "vital", false);

            serializer.DataField(ref _mechanismIds, "mechanisms", new List<string>());
        }

        public override void Initialize()
        {
            base.Initialize();

            // TODO move this to server, same for body parts in body component
            foreach (var mechanismId in _mechanismIds)
            {
                var mechanism = Owner.EntityManager.SpawnEntity(mechanismId, Owner.Transform.MapPosition);
                var mechanismComponent = mechanism.GetComponent<IMechanism>();

                TryInstallMechanism(mechanismComponent, true);
            }
        }

        public bool Drop()
        {
            Body = null;
            Owner.AttachToGrandparent();
            return true;
        }

        public bool SurgeryCheck(SurgeryType surgery)
        {
            return SurgeryDataComponent?.CheckSurgery(surgery) ?? false;
        }

        /// <summary>
        ///     Attempts to perform surgery on this <see cref="IBodyPart"/> with the given
        ///     tool.
        /// </summary>
        /// <returns>True if successful, false if there was an error.</returns>
        public bool AttemptSurgery(SurgeryType toolType, IBodyPartContainer target, ISurgeon surgeon, IEntity performer)
        {
            DebugTools.AssertNotNull(toolType);
            DebugTools.AssertNotNull(target);
            DebugTools.AssertNotNull(surgeon);
            DebugTools.AssertNotNull(performer);

            return SurgeryDataComponent?.PerformSurgery(toolType, target, surgeon, performer) ?? false;
        }

        public bool CanAttachPart(IBodyPart part)
        {
            DebugTools.AssertNotNull(part);

            return SurgeryDataComponent?.CanAttachBodyPart(part) ?? false;
        }

        public bool CanInstallMechanism(IMechanism mechanism)
        {
            DebugTools.AssertNotNull(mechanism);

            return SurgeryDataComponent != null &&
                   SizeUsed + mechanism.Size <= Size &&
                   SurgeryDataComponent.CanInstallMechanism(mechanism);
        }

        /// <summary>
        ///     Tries to install a mechanism onto this body part.
        /// </summary>
        /// <param name="mechanism">The mechanism to try to install.</param>
        /// <param name="force">
        ///     Whether or not to check if the mechanism can be installed.
        /// </param>
        /// <returns>
        ///     True if successful, false if there was an error
        ///     (e.g. not enough room in <see cref="IBodyPart"/>).
        ///     Will return false even when forced if the mechanism is already
        ///     installed in this <see cref="IBodyPart"/>.
        /// </returns>
        public bool TryInstallMechanism(IMechanism mechanism, bool force = false)
        {
            DebugTools.AssertNotNull(mechanism);

            if (!force && !CanInstallMechanism(mechanism))
            {
                return false;
            }

            if (!_mechanisms.Add(mechanism))
            {
                return false;
            }

            OnMechanismAdded(mechanism);
            return true;
        }

        private void OnMechanismAdded(IMechanism mechanism)
        {
            var prototypeId = mechanism.Owner.Prototype!.ID;
            if (!_mechanismIds.Contains(prototypeId))
            {
                _mechanismIds.Add(prototypeId);
            }

            mechanism.Part = this;
            SizeUsed += mechanism.Size;
        }

        public bool RemoveMechanism(IMechanism mechanism)
        {
            DebugTools.AssertNotNull(mechanism);

            if (!_mechanisms.Remove(mechanism))
            {
                return false;
            }

            _mechanismIds.Remove(mechanism.Owner.Prototype!.ID);
            mechanism.Part = null;
            SizeUsed -= mechanism.Size;

            return true;
        }

        public bool RemoveMechanism(IMechanism mechanism, EntityCoordinates coordinates)
        {
            if (RemoveMechanism(mechanism))
            {
                mechanism.Owner.Transform.Coordinates = coordinates;
                return true;
            }

            return false;
        }

        public bool DeleteMechanism(IMechanism mechanism)
        {
            DebugTools.AssertNotNull(mechanism);

            if (!RemoveMechanism(mechanism))
            {
                return false;
            }

            mechanism.Owner.Delete();
            return true;
        }

        private void OnHealthChanged(List<HealthChangeData> changes)
        {
            // TODO
        }

        public bool ShowContextMenu(IEntity examiner)
        {
            return Body == null;
        }

        public bool CanExamine(IEntity entity)
        {
            return Body == null;
        }
    }
}
