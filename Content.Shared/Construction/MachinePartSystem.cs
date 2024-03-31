using System.Linq;
using Content.Shared.Construction.Components;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Examine;
using Content.Shared.Lathe;
using Content.Shared.Materials;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.Shared.Construction
{
    /// <summary>
    /// Deals with machine parts and machine boards.
    /// </summary>
    public sealed class MachinePartSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        [Dependency] private readonly SharedLatheSystem _lathe = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MachineBoardComponent, ExaminedEvent>(OnMachineBoardExamined);
        }

        private void OnMachineBoardExamined(EntityUid uid, MachineBoardComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

            using (args.PushGroup(nameof(MachineBoardComponent)))
            {
                args.PushMarkup(Loc.GetString("machine-board-component-on-examine-label"));
                foreach (var (part, amount) in component.Requirements)
                {
                    args.PushMarkup(Loc.GetString("machine-board-component-required-element-entry-text",
                        ("amount", amount),
                        ("requiredElement", Loc.GetString(_prototype.Index<MachinePartPrototype>(part).Name))));
                }

                foreach (var (material, amount) in component.MaterialRequirements)
                {
                    args.PushMarkup(Loc.GetString("machine-board-component-required-element-entry-text",
                        ("amount", amount),
                        ("requiredElement", Loc.GetString(material.Name))));
                }

                foreach (var (_, info) in component.ComponentRequirements)
                {
                    args.PushMarkup(Loc.GetString("machine-board-component-required-element-entry-text",
                        ("amount", info.Amount),
                        ("requiredElement", Loc.GetString(info.ExamineName))));
                }

                foreach (var (_, info) in component.TagRequirements)
                {
                    args.PushMarkup(Loc.GetString("machine-board-component-required-element-entry-text",
                        ("amount", info.Amount),
                        ("requiredElement", Loc.GetString(info.ExamineName))));
                }
            }
        }

        public Dictionary<string, int> GetMachineBoardMaterialCost(Entity<MachineBoardComponent> entity, int coefficient = 1)
        {
            var (_, comp) = entity;

            var materials = new Dictionary<string, int>();
            foreach (var (partId, amount) in comp.Requirements)
            {
                var partProto = _prototype.Index<MachinePartPrototype>(partId);

                if (!_lathe.TryGetRecipesFromEntity(partProto.StockPartPrototype, out var recipes))
                    continue;

                var partRecipe = recipes[0];
                if (recipes.Count > 1)
                    partRecipe = recipes.MinBy(p => p.RequiredMaterials.Values.Sum());

                foreach (var (mat, matAmount) in partRecipe!.RequiredMaterials)
                {
                    materials.TryAdd(mat, 0);
                    materials[mat] += matAmount * amount * coefficient;
                }
            }

            foreach (var (stackId, amount) in comp.MaterialIdRequirements)
            {
                var stackProto = _prototype.Index<StackPrototype>(stackId);

                if (_prototype.TryIndex(stackProto.Spawn, out var defaultProto) &&
                    defaultProto.TryGetComponent<PhysicalCompositionComponent>(out var physComp))
                {
                    foreach (var (mat, matAmount) in physComp.MaterialComposition)
                    {
                        materials.TryAdd(mat, 0);
                        materials[mat] += matAmount * amount * coefficient;
                    }
                }
                else if (_lathe.TryGetRecipesFromEntity(stackProto.Spawn, out var recipes))
                {
                    var partRecipe = recipes[0];
                    if (recipes.Count > 1)
                        partRecipe = recipes.MinBy(p => p.RequiredMaterials.Values.Sum());

                    foreach (var (mat, matAmount) in partRecipe!.RequiredMaterials)
                    {
                        materials.TryAdd(mat, 0);
                        materials[mat] += matAmount * amount * coefficient;
                    }
                }
            }

            var genericPartInfo = comp.ComponentRequirements.Values.Concat(comp.ComponentRequirements.Values);
            foreach (var info in genericPartInfo)
            {
                var amount = info.Amount;
                var defaultProtoId = info.DefaultPrototype;

                if (_lathe.TryGetRecipesFromEntity(defaultProtoId, out var recipes))
                {
                    var partRecipe = recipes[0];
                    if (recipes.Count > 1)
                        partRecipe = recipes.MinBy(p => p.RequiredMaterials.Values.Sum());

                    foreach (var (mat, matAmount) in partRecipe!.RequiredMaterials)
                    {
                        materials.TryAdd(mat, 0);
                        materials[mat] += matAmount * amount * coefficient;
                    }
                }
                else if (_prototype.TryIndex(defaultProtoId, out var defaultProto) &&
                         defaultProto.TryGetComponent<PhysicalCompositionComponent>(out var physComp))
                {
                    foreach (var (mat, matAmount) in physComp.MaterialComposition)
                    {
                        materials.TryAdd(mat, 0);
                        materials[mat] += matAmount * amount * coefficient;
                    }
                }
            }

            return materials;
        }
    }
}
