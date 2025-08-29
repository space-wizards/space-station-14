namespace Content.Shared._Starlight.Character.Info;

public readonly record struct OpenInspectCharacterInfoEvent(EntityUid Target, EntityUid Viewer);