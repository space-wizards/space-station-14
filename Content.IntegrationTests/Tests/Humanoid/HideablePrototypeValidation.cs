using System.Collections.Generic;
using System.Linq;
using Content.Shared.Body;
using Content.Shared.Clothing.Components;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Humanoid;

[TestFixture]
public sealed class HideablePrototypeValidation
{
    [Test]
    public async Task NoOrgansWithoutClothing()
    {
        await using var pair = await PoolManager.GetServerClient();

        var requirements = new Dictionary<Enum, HashSet<EntProtoId>>();
        foreach (var (proto, component) in pair.GetPrototypesWithComponent<VisualOrganMarkingsComponent>())
        {
            foreach (var layer in component.HideableLayers)
            {
                requirements[layer] = requirements.GetValueOrDefault(layer) ?? [];
                requirements[layer].Add(proto.ID);
            }
        }

        var provided = new HashSet<HumanoidVisualLayers>();
        foreach (var (_, component) in pair.GetPrototypesWithComponent<HideLayerClothingComponent>())
        {
#pragma warning disable CS0618 // Type or member is obsolete
            if (component.Slots is { } slots)
            {
                provided.UnionWith(slots);
            }
            provided.UnionWith(component.Layers.Keys);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        using var scope = Assert.EnterMultipleScope();
        foreach (var (key, requirement) in requirements)
        {
            Assert.That(provided, Does.Contain(key), $"No clothing will hide {key} that can be hidden on {string.Join(", ", requirement.Select(it => it.Id))}");
        }

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task NoClothingWithoutOrgans()
    {
        await using var pair = await PoolManager.GetServerClient();

        var requirements = new Dictionary<Enum, HashSet<EntProtoId>>();
        foreach (var (proto, component) in pair.GetPrototypesWithComponent<HideLayerClothingComponent>())
        {
#pragma warning disable CS0618 // Type or member is obsolete
            foreach (var layer in component.Layers.Keys.Concat(component.Slots ?? []))
#pragma warning restore CS0618 // Type or member is obsolete
            {
                requirements[layer] = requirements.GetValueOrDefault(layer) ?? [];
                requirements[layer].Add(proto.ID);
            }
        }

        var provided = new HashSet<Enum>();
        foreach (var (_, component) in pair.GetPrototypesWithComponent<VisualOrganMarkingsComponent>())
        {
            provided.UnionWith(component.HideableLayers);
        }

        using var scope = Assert.EnterMultipleScope();
        foreach (var (key, requirement) in requirements)
        {
            Assert.That(provided, Does.Contain(key), $"No organ will hide {key} that can be hidden by {string.Join(", ", requirement.Select(it => it.Id))}");
        }

        await pair.CleanReturnAsync();
    }
}
