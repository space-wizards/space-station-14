using Content.Shared.Store;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(HereticRuleSystem))]
public sealed partial class HereticRuleComponent : Component
{
    public readonly List<EntityUid> Minds = new();

    public readonly List<ProtoId<StoreCategoryPrototype>> StoreCategories = new()
    {
        "HereticPathAsh",
        //"HereticPathLock", //TODO
        "HereticPathFlesh",
        //"HereticPathBlade", //TODO
        "HereticPathVoid",
        //"HereticPathRust", //TODO
        "HereticPathSide"
    };
}
