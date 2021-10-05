using System;
using System.Collections.Generic;
using Content.Server.Access.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.PDA
{
    [RegisterComponent]
    [ComponentReference(typeof(IAccess))]
    public class PDAComponent : Component, IAccess
    {
        public override string Name => "PDA";

        public const string IDSlotName = "pda_id_slot";
        public const string PenSlotName = "pda_pen_slot";

        [ViewVariables] [DataField("idCard")] public string? StartingIdCard;

        [ViewVariables] public IdCardComponent? ContainedID;
        [ViewVariables] public bool PenInserted;
        [ViewVariables] public bool FlashlightOn;

        [ViewVariables] public string? OwnerName;

        // TODO: Move me to ECS after Access refactoring
        #region Acces Logic
        [ViewVariables] private readonly PDAAccessSet _accessSet;

        public PDAComponent()
        {
            _accessSet = new PDAAccessSet(this);
        }

        public ISet<string>? GetContainedAccess()
        {
            return ContainedID?.Owner?.GetComponent<AccessComponent>()?.Tags;
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
