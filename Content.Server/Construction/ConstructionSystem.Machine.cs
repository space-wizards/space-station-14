using System.Linq;
using Content.Server.Construction.Components;
using Content.Server.Examine;
using Content.Shared.Construction.Components;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Server.Construction;

public sealed partial class ConstructionSystem
{
    [Dependency] private readonly ExamineSystem _examineSystem = default!;

    private void InitializeMachines()
    {
        SubscribeLocalEvent<MachineComponent, ComponentInit>(OnMachineInit);
        SubscribeLocalEvent<MachineComponent, MapInitEvent>(OnMachineMapInit);
        SubscribeLocalEvent<MachineComponent, GetVerbsEvent<ExamineVerb>>(OnMachineExaminableVerb);
    }

    private void OnMachineInit(EntityUid uid, MachineComponent component, ComponentInit args)
    {
        component.BoardContainer = _container.EnsureContainer<Container>(uid, MachineFrameComponent.BoardContainerName);
        component.PartContainer = _container.EnsureContainer<Container>(uid, MachineFrameComponent.PartContainerName);
    }

    private void OnMachineMapInit(EntityUid uid, MachineComponent component, MapInitEvent args)
    {
        CreateBoardAndStockParts(uid, component);
        RefreshParts(uid, component);
    }

    private void OnMachineExaminableVerb(EntityUid uid, MachineComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var markup = new FormattedMessage();
        RaiseLocalEvent(uid, new UpgradeExamineEvent(ref markup));
        if (markup.IsEmpty)
            return; // Not upgradable.

        markup = FormattedMessage.FromMarkup(markup.ToMarkup().TrimEnd('\n')); // Cursed workaround to https://github.com/space-wizards/RobustToolbox/issues/3371

        var verb = new ExamineVerb()
        {
            Act = () =>
            {
                _examineSystem.SendExamineTooltip(args.User, uid, markup, getVerbs: false, centerAtCursor: false);
            },
            Text = Loc.GetString("machine-upgrade-examinable-verb-text"),
            Message = Loc.GetString("machine-upgrade-examinable-verb-message"),
            Category = VerbCategory.Examine,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/pickup.svg.192dpi.png"))
        };

        args.Verbs.Add(verb);
    }

    public List<MachinePartComponent> GetAllParts(EntityUid uid, MachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return new List<MachinePartComponent>();

        return GetAllParts(component);
    }

    public List<MachinePartComponent> GetAllParts(MachineComponent component)
    {
        var parts = new List<MachinePartComponent>();

        foreach (var entity in component.PartContainer.ContainedEntities)
        {
            if (TryComp<MachinePartComponent>(entity, out var machinePart))
                parts.Add(machinePart);
        }

        return parts;
    }

    public Dictionary<string, float> GetPartsRatings(List<MachinePartComponent> parts)
    {
        var output = new Dictionary<string, float>();
        foreach (var type in _prototypeManager.EnumeratePrototypes<MachinePartPrototype>())
        {
            var amount = 0f;
            var sumRating = 0f;
            foreach (var part in parts.Where(part => part.PartType == type.ID))
            {
                amount++;
                sumRating += part.Rating;
            }
            var rating = amount != 0 ? sumRating / amount : 0;
            output.Add(type.ID, rating);
        }

        return output;
    }

    public void RefreshParts(EntityUid uid, MachineComponent component)
    {
        var parts = GetAllParts(component);
        EntityManager.EventBus.RaiseLocalEvent(uid, new RefreshPartsEvent
        {
            Parts = parts,
            PartRatings = GetPartsRatings(parts),
        }, true);
    }

    private void CreateBoardAndStockParts(EntityUid uid, MachineComponent component)
    {
        // Entity might not be initialized yet.
        var boardContainer = _container.EnsureContainer<Container>(uid, MachineFrameComponent.BoardContainerName);
        var partContainer = _container.EnsureContainer<Container>(uid, MachineFrameComponent.PartContainerName);

        if (string.IsNullOrEmpty(component.BoardPrototype))
            return;

        // We're done here, let's suppose all containers are correct just so we don't screw SaveLoadSave.
        if (boardContainer.ContainedEntities.Count > 0)
            return;

        var board = EntityManager.SpawnEntity(component.BoardPrototype, Transform(uid).Coordinates);

        if (!_container.Insert(board, component.BoardContainer))
        {
            throw new Exception($"Couldn't insert board with prototype {component.BoardPrototype} to machine with prototype {MetaData(uid).EntityPrototype?.ID ?? "N/A"}!");
        }

        if (!TryComp<MachineBoardComponent>(board, out var machineBoard))
        {
            throw new Exception($"Entity with prototype {component.BoardPrototype} doesn't have a {nameof(MachineBoardComponent)}!");
        }

        var xform = Transform(uid);
        foreach (var (part, amount) in machineBoard.Requirements)
        {
            var partProto = _prototypeManager.Index<MachinePartPrototype>(part);
            for (var i = 0; i < amount; i++)
            {
                var p = EntityManager.SpawnEntity(partProto.StockPartPrototype, xform.Coordinates);

                if (!_container.Insert(p, partContainer))
                    throw new Exception($"Couldn't insert machine part of type {part} to machine with prototype {partProto.StockPartPrototype}!");
            }
        }

        foreach (var (stackType, amount) in machineBoard.MaterialRequirements)
        {
            var stack = _stackSystem.Spawn(amount, stackType, Transform(uid).Coordinates);

            if (!_container.Insert(stack, partContainer))
                throw new Exception($"Couldn't insert machine material of type {stackType} to machine with prototype {MetaData(uid).EntityPrototype?.ID ?? "N/A"}");
        }

        foreach (var (compName, info) in machineBoard.ComponentRequirements)
        {
            for (var i = 0; i < info.Amount; i++)
            {
                var c = EntityManager.SpawnEntity(info.DefaultPrototype, Transform(uid).Coordinates);

                if(!_container.Insert(c, partContainer))
                    throw new Exception($"Couldn't insert machine component part with default prototype '{compName}' to machine with prototype {MetaData(uid).EntityPrototype?.ID ?? "N/A"}");
            }
        }

        foreach (var (tagName, info) in machineBoard.TagRequirements)
        {
            for (var i = 0; i < info.Amount; i++)
            {
                var c = EntityManager.SpawnEntity(info.DefaultPrototype, Transform(uid).Coordinates);

                if(!_container.Insert(c, partContainer))
                    throw new Exception($"Couldn't insert machine component part with default prototype '{tagName}' to machine with prototype {MetaData(uid).EntityPrototype?.ID ?? "N/A"}");
            }
        }
    }
}

public sealed class RefreshPartsEvent : EntityEventArgs
{
    public IReadOnlyList<MachinePartComponent> Parts = new List<MachinePartComponent>();

    public Dictionary<string, float> PartRatings = new();
}

public sealed class UpgradeExamineEvent : EntityEventArgs
{
    private FormattedMessage Message;

    public UpgradeExamineEvent(ref FormattedMessage message)
    {
        Message = message;
    }

    /// <summary>
    /// Add a line to the upgrade examine tooltip with a percentage-based increase or decrease.
    /// </summary>
    public void AddPercentageUpgrade(string upgradedLocId, float multiplier)
    {
        var percent = Math.Round(100 * MathF.Abs(multiplier - 1), 2);
        var locId = multiplier switch {
            < 1 => "machine-upgrade-decreased-by-percentage",
            1 or float.NaN => "machine-upgrade-not-upgraded",
            > 1 => "machine-upgrade-increased-by-percentage",
        };
        var upgraded = Loc.GetString(upgradedLocId);
        this.Message.AddMarkup(Loc.GetString(locId, ("upgraded", upgraded), ("percent", percent)) + '\n');
    }

    /// <summary>
    /// Add a line to the upgrade examine tooltip with a numeric increase or decrease.
    /// </summary>
    public void AddNumberUpgrade(string upgradedLocId, int number)
    {
        var difference = Math.Abs(number);
        var locId = number switch {
            < 0 => "machine-upgrade-decreased-by-amount",
            0 => "machine-upgrade-not-upgraded",
            > 0 => "machine-upgrade-increased-by-amount",
        };
        var upgraded = Loc.GetString(upgradedLocId);
        this.Message.AddMarkup(Loc.GetString(locId, ("upgraded", upgraded), ("difference", difference)) + '\n');
    }
}
