using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Tag;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Server.Ninja.Components;

[RegisterComponent]
public sealed class SpaceNinjaGlovesComponent : Component
{
    /// <summary>
    /// The action for emagging stuff with ninja gloves
    /// </summary>
    [DataField("emagAction")]
    public EntityTargetAction EmagAction = new()
    {
          UseDelay = TimeSpan.FromSeconds(1), // can't spam it ridiclously fast
          Icon = new SpriteSpecifier.Rsi(new ResourcePath("Objects/Tools/emag.rsi"), "icon"),
          ItemIconStyle = ItemActionIconStyle.BigAction,
          DisplayName = "action-name-emag",
          Description = "action-desc-emag",
          Event = new NinjaEmagEvent()
      };

    /// <summary>
    /// The tag that marks an entity as immune to emags
    /// </summary>
    [DataField("emagImmuneTag", customTypeSerializer: typeof(PrototypeIdSerializer<TagPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string EmagImmuneTag = "EmagImmune";
}

public sealed class NinjaEmagEvent : EntityTargetActionEvent { }
