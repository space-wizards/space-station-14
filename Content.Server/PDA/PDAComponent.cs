using System;
using System.Collections.Generic;
using Content.Server.Access.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.PDA
{
    [RegisterComponent]
    [ComponentReference(typeof(IAccess))]
    public class PDAComponent : Component, IAccess
    {
        public override string Name => "PDA";

        [DataField("idSlot", required: true)]
        public ItemSlot IdSlot = default!;

        [DataField("penSlot", required: true)]
        public ItemSlot PenSlot = default!;

        // Really this should just be using ItemSlot.StartingItem. However, seeing as we have so many different starting
        // PDA's and no nice way to inherit the other fields from the ItemSlot data definition, this makes the yaml much
        // nicer to read.
        [DataField("idCard", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? IdCard;

        [ViewVariables] public IdCardComponent? ContainedID;
        [ViewVariables] public bool FlashlightOn;

        [ViewVariables] public string? OwnerName;

        // TODO: Move me to ECS after Access refactoring
        #region Access Logic
        [ViewVariables] private readonly PDAAccessSet _accessSet;

        public PDAComponent()
        {
            _accessSet = new PDAAccessSet(this);
        }

        public ISet<string>? GetContainedAccess()
        {
            return IdSlot.Item?.GetComponent<AccessComponent>()?.Tags;
        }

        ISet<string> IAccess.Tags => _accessSet;

        bool IAccess.IsReadOnly => true;

        void IAccess.SetTags(IEnumerable<string> newTags)
        {
            throw new NotSupportedException("PDA access list is read-only.");
        }
        #endregion
    }
}
