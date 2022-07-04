using Content.Server.Singularity.Components;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Content.Server.Singularity.EntitySystems;

public sealed class ContainmentFieldConnectionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

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

    private void OnConnect(ContainmentFieldComponent component)
    {
        if (component.Generator1 == null || component.Generator2 == null)
            return;

        var gen1Coords = Transform(component.Generator1.Value).Coordinates;
        var gen2Coords = Transform(component.Generator2.Value).Coordinates;

        var delta = (gen2Coords - gen1Coords).Position;
        var dirVec = delta.Normalized;
        var stopDist = delta.Length;
        var currentOffset = dirVec;
        while (currentOffset.Length < stopDist)
        {
            var currentCoords = gen1Coords.Offset(currentOffset);
            var newField = Spawn(component.CreatedField, currentCoords);

            var fieldXForm = Transform(newField);
            fieldXForm.AttachParent(component.Generator1.Value);

            component.Fields.Add(newField);
            currentOffset += dirVec;
        }
    }
}
