### Localization used for the invoke verb command.
# Mostly help + error messages.

invoke-verb-command-description = Invokes a verb with the given name on an entity, with the player entity
invoke-verb-command-help = invokeverb <playerUid | "self"> <targetUid> <verbName | "interaction" | "activation" | "alternative">

invoke-verb-command-invalid-args = invokeverb takes 2 arguments.

invoke-verb-command-invalid-player-uid = Player uid could not be parsed, or "self" was not passed.
invoke-verb-command-invalid-target-uid = Target uid could not be parsed.

invoke-verb-command-invalid-player-entity = Player uid given does not correspond to a valid entity.
invoke-verb-command-invalid-target-entity = Target uid given does not correspond to a valid entity.

invoke-verb-command-success = Invoked verb '{ $verb }' on { $target } with { $player } as the user.

invoke-verb-command-verb-not-found = Could not find verb { $verb } on { $target }.
