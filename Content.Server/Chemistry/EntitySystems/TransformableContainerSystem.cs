using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class TransformableContainerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionsSystem = default!;
    [Dependency] private readonly MetaDataSystem _metadataSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TransformableContainerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TransformableContainerComponent, SolutionChangedEvent>(OnSolutionChange);
    }

    private void OnMapInit(EntityUid uid, TransformableContainerComponent component, MapInitEvent args)
    {
        var meta = MetaData(uid);
        if (string.IsNullOrEmpty(component.InitialName))
        {
            component.InitialName = meta.EntityName;
        }
        if (string.IsNullOrEmpty(component.InitialDescription))
        {
            component.InitialDescription = meta.EntityDescription;
        }
    }

    private void OnSolutionChange(EntityUid owner, TransformableContainerComponent component,
        SolutionChangedEvent args)
    {
        if (!_solutionsSystem.TryGetFitsInDispenser(owner, out var solution))
            return;
        //Transform container into initial state when emptied
        if (component.CurrentReagent != null && solution.Contents.Count == 0)
        {
            CancelTransformation(owner, component);
        }

        //the biggest reagent in the solution decides the appearance
        var reagentId = solution.GetPrimaryReagentId();

        //If biggest reagent didn't changed - don't change anything at all
        if (component.CurrentReagent != null && component.CurrentReagent.ID == reagentId?.Prototype)
        {
            return;
        }

        //Only reagents with spritePath property can change appearance of transformable containers!
        if (!string.IsNullOrWhiteSpace(reagentId?.Prototype)
            && _prototypeManager.TryIndex(reagentId.Value.Prototype, out ReagentPrototype? proto))
        {
            var metadata = MetaData(owner);
            var val = Loc.GetString("transformable-container-component-glass", ("name", proto.LocalizedName));
            _metadataSystem.SetEntityName(owner, val, metadata);
            _metadataSystem.SetEntityDescription(owner, proto.LocalizedDescription, metadata);
            component.CurrentReagent = proto;
            component.Transformed = true;
        }
    }

    private void CancelTransformation(EntityUid owner, TransformableContainerComponent component)
    {
        component.CurrentReagent = null;
        component.Transformed = false;

        var metadata = MetaData(owner);

        if (!string.IsNullOrEmpty(component.InitialName))
        {
            _metadataSystem.SetEntityName(owner, component.InitialName, metadata);
        }
        if (!string.IsNullOrEmpty(component.InitialDescription))
        {
            _metadataSystem.SetEntityDescription(owner, component.InitialDescription, metadata);
        }
    }
}
