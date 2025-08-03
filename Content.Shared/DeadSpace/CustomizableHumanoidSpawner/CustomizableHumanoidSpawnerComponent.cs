// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Dataset;
using Robust.Shared.Serialization;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Roles;
using Content.Shared.Tag;
using Robust.Shared.Enums;

namespace Content.Shared.DeadSpace.CustomizableHumanoidSpawner;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class CustomizableHumanoidSpawnerComponent : Component
{
    /// <summary>
    /// Отключает настройку персонажа и сразу спавнит рандомного. Может пригодится когда нужен только функционал добавления тегов и фракций.
    /// </summary>
    [DataField]
    public bool ForceRandom;

    /// <summary>
    /// Прототип должности которая будет заспавнена.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<JobPrototype> JobPrototype;

    /// <summary>
    /// Разрешённые расы.
    /// </summary>
    [DataField(serverOnly: true)]
    public HashSet<ProtoId<SpeciesPrototype>> AllowedSpecies = [];

    /// <summary>
    /// Датасет со списком имен для первой части имени.
    /// </summary>
    [DataField(serverOnly: true)]
    public ProtoId<DatasetPrototype>? RandomNameFirstDataset;

    /// <summary>
    /// Датасет со списком имен для второй части имени (для имени из одного слова - укажите только датасет первой части).
    /// </summary>
    [DataField(serverOnly: true)]
    public ProtoId<DatasetPrototype>? RandomNameSecondDataset;

    /// <summary>
    /// Альтернативный вариант указания первой части имени через локализованный датасет. Игнорируется если указан обычный датасет.
    /// </summary>
    [DataField(serverOnly: true)]
    public ProtoId<LocalizedDatasetPrototype>? RandomNameFirstLocalized;

    /// <summary>
    /// Альтернативный вариант указания второй части имени через локализованный датасет. Игнорируется если указан обычный датасет.
    /// </summary>
    [DataField(serverOnly: true)]
    public ProtoId<LocalizedDatasetPrototype>? RandomNameSecondLocalized;

    /// <summary>
    /// Список тегов которые будут добавлены к персонажу.
    /// </summary>
    [DataField(serverOnly: true)]
    public List<ProtoId<TagPrototype>>? Tags;

    /// <summary>
    /// Список фракций которые будут добавлены к персонажу.
    /// </summary>
    [DataField(serverOnly: true)]
    public HashSet<ProtoId<NpcFactionPrototype>>? Factions;
}

[Serializable, NetSerializable]
public sealed class CustomizableHumanoidSpawnerCharacterInfo(
    string name,
    string description,
    ProtoId<SpeciesPrototype> species,
    Gender gender,
    int index)
{
    public string Name = name;
    public string Description = description;
    public ProtoId<SpeciesPrototype> Species = species;
    public Gender Gender = gender;
    public int Index = index;
}

[Serializable, NetSerializable]
public sealed class CustomizableHumanoidSpawnerBuiState(
    List<CustomizableHumanoidSpawnerCharacterInfo> characters,
    bool canChangeNameAndDescription,
    string? randomizedName,
    HashSet<ProtoId<SpeciesPrototype>> allowedSpecies) : BoundUserInterfaceState
{
    public readonly List<CustomizableHumanoidSpawnerCharacterInfo> Characters = characters;
    public readonly bool CanChangeNameAndDescription = canChangeNameAndDescription;
    public readonly string? RandomizedName = randomizedName;
    public readonly HashSet<ProtoId<SpeciesPrototype>> AllowedSpecies = allowedSpecies;
}

[Serializable, NetSerializable]
public sealed class CustomizableHumanoidSpawnerMessage(
    bool useRandom,
    int characterIndex,
    string customName,
    bool useCustomDescription,
    string customDescription) : BoundUserInterfaceMessage
{
    public bool UseRandom = useRandom;
    public int CharacterIndex = characterIndex;
    public string CustomName = customName;
    public bool UseCustomDescription = useCustomDescription;
    public string CustomDescription = customDescription;
}

[Serializable, NetSerializable]
public enum CustomizableHumanoidSpawnerUiKey : byte
{
    Key,
}
