using Content.Client.Body.Systems;
using Content.Client.UserInterface.Systems.MedicalMenu.Controls;
using Content.Client.UserInterface.Systems.MedicalMenu.Windows;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Medical.Wounding.Components;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.UserInterface.Systems.MedicalMenu;

public sealed class MedicalMenuUIController : UIController
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [UISystemDependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    private MedicalMenuWindow _medicalWindow = default!;
    private bool _isOpen = false;
    public bool IsOpen => _isOpen;
    private EntityUid? _target;
    public EntityUid? CurrentTarget => _target;

    private BodyPartStatusControl? bodyPartStatusTree = null;
    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        _medicalWindow = UIManager.CreateWindow<MedicalMenuWindow>();
        _medicalWindow.OnClose += CloseWindow;
        _sawmill = _logManager.GetSawmill("Medical.Menu");
    }

    public void OpenWindow()
    {
        _target ??= _playerManager.LocalSession?.AttachedEntity;
        OpenWindow(_target);
    }

    public void OpenWindow(EntityUid? target)
    {
        SetTarget(target);
        _medicalWindow.Open();
        _isOpen = true;
    }

    public void CloseWindow()
    {
        _medicalWindow.Close();
        _isOpen = false;
    }

    private void ClearBodyStatusTree()
    {
        if (bodyPartStatusTree == null)
            return;
        bodyPartStatusTree.Parent?.RemoveChild(bodyPartStatusTree);
        bodyPartStatusTree = null;
    }

    private BodyPartStatusControl CreateBodyPartStatusLeaf(
        EntityUid partEntity,
        BodyPartComponent bodyPart,
        WoundableComponent woundable)
    {
        var newStatusLeaf = new BodyPartStatusControl();
        newStatusLeaf.LinkPart(new Entity<BodyPartComponent,WoundableComponent>(partEntity, bodyPart, woundable));
        newStatusLeaf.PartName = EntityManager.GetComponent<MetaDataComponent>(partEntity).EntityName;
        return newStatusLeaf;
    }

    private void RecursivelyAddChildParts(BodyPartStatusControl parentControl, EntityUid parentPartEnt, BodyPartComponent parentPart)
    {
        if (parentControl.LinkedPart == null)
            return;

        foreach (var (childPartEnt, childPart) in
                 _bodySystem.GetBodyPartDirectChildren(parentPartEnt, parentPart))
        {
            if (!EntityManager.TryGetComponent<WoundableComponent>(childPartEnt, out var woundable))
                continue;
            var leaf = CreateBodyPartStatusLeaf(childPartEnt, childPart, woundable);
            parentControl.AddChildPart(leaf);
            RecursivelyAddChildParts(leaf, childPartEnt, childPart);
        }
    }

    //Recreates the part status tree, this should be called when parts are attached or detached
    public void RefreshPartStatusTree()
    {
        if (bodyPartStatusTree == null)
            return;
        var oldData = bodyPartStatusTree.LinkedPart;
        ClearBodyStatusTree();
        bodyPartStatusTree = CreateBodyPartStatusLeaf(oldData!.Value.Owner, oldData.Value.Comp1, oldData.Value.Comp2);
        RecursivelyAddChildParts(bodyPartStatusTree, oldData.Value.Owner, oldData.Value.Comp1);
    }


    private void CreateBodyPartStatusTree(EntityUid target, BodyComponent body)
    {
        if (bodyPartStatusTree != null)
        {
            //This should never happen unless someone is doing something stupid and in which case this error message is for YOU!!!
            _sawmill.Error("Tried to create body part status tree when one already exists!");
            return;
        }

        if (!_bodySystem.TryGetRootBodyPart(target, out var rootPart, body)
            || !EntityManager.TryGetComponent<WoundableComponent>(rootPart.Value.Owner, out var woundable)
            )
            return;
        bodyPartStatusTree = CreateBodyPartStatusLeaf(rootPart.Value.Owner, rootPart, woundable);
        RecursivelyAddChildParts(bodyPartStatusTree, rootPart.Value.Owner, rootPart);
        _medicalWindow.OverviewTab.AddChildPart(bodyPartStatusTree);
    }


    public void SetTarget(EntityUid? targetEntity)
    {
        if (targetEntity == null || _target == targetEntity)
            return;
        ClearBodyStatusTree();
        if (!EntityManager.TryGetComponent<BodyComponent>(targetEntity.Value, out var body))
        {
            _target = null;
            _medicalWindow.OverviewTab.TargetStatus.SetTarget(null);
            return;
        }

        _target = targetEntity;
        var targetEntName = EntityManager.GetComponent<MetaDataComponent>(targetEntity.Value).EntityName;
        _medicalWindow.OverviewTab.TargetStatus.SetTarget(targetEntName);
        CreateBodyPartStatusTree(targetEntity.Value, body);
    }

}
