using Content.Server.Singularity.Components;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Content.Server.Singularity.EntitySystems;

public sealed class ContainmentFieldConnectionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ContainmentFieldComponent, ContainmentFieldConnectEvent>(OnConnect);
    }

    public void PopulateGenerators(ContainmentFieldGeneratorComponent generator1, ContainmentFieldGeneratorComponent generator2)
    {

    }

    public void DeleteFields(ContainmentFieldComponent component)
    {
        foreach (var field in component.Fields)
        {
            QueueDel(field);
        }
        component.Fields.Clear();

        component.Generator1 = null;
        component.Generator2 = null;
    }

    private void OnConnect(EntityUid uid, ContainmentFieldComponent component, ContainmentFieldConnectEvent args)
    {
        if (component.Generator1 == null || component.Generator2 == null)
            return;

        var gen1XForm = Transform(component.Generator1.Value);
        var gen2XForm = Transform(component.Generator2.Value);

        var gen1Coords = gen1XForm.Coordinates;
        var gen2Coords = gen2XForm.Coordinates;
    }
}
