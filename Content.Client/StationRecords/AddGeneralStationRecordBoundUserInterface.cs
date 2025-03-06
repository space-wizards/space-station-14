using System.Linq;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.StationRecords;

[UsedImplicitly]
public sealed class AddGeneralStationRecordBoundUserInterface(EntityUid owner, Enum uiKey)
    : BoundUserInterface(owner, uiKey)
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    [ViewVariables]
    private AddGeneralStationRecord? _window = default!;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<AddGeneralStationRecord>();
        _window.SetSelectableJobs(_prototypeManager.EnumeratePrototypes<JobPrototype>().ToList());
        _window.SetSelectableSpecies(_prototypeManager.EnumeratePrototypes<SpeciesPrototype>().ToList());
        _window.OnValidSubmit += record =>
        {
            SendMessage(new AddStationRecordMessage(record));
            _window.Close();
        };
    }
}
