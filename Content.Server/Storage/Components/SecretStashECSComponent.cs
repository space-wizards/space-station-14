using Content.Shared.Item;
using Content.Shared.Sound;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Storage.Components
{
    [RegisterComponent]
    public class SecretStashECSComponent : Component
    {
        public override string Name => "SecretStashECS";

        [ViewVariables]
        [DataField("maxItemSize")]
        public int MaxItemSize { get; set; } = (int) ReferenceSizes.Pocket;

        [ViewVariables]
        [DataField("secretPartName")]
        public string? SecretPartNameOverride { get; private set; } = default;

        [ViewVariables] public ContainerSlot ItemContainer { get; set; } = default!;

        public string SecretPartName => SecretPartNameOverride ?? Loc.GetString("comp-secret-stash-secret-part-name", ("name", Owner.Name));

        [ViewVariables]
        [DataField("interactionSound")]
        public SoundSpecifier? InteractionSound { get; private set; } = default;

        [ViewVariables]
        [DataField("noItemOnInteractMessage")]
        public string? NoItemOnInteractMessage { get; private set; } = default;
    }
}
