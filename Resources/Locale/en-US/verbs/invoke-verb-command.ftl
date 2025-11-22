### Localization used for the invoke verb command.
# Mostly help + error messages.

cmd-invokeverb-desc = Invokes a verb with the given name on an entity, with the player entity
cmd-invokeverb-help = invokeverb <playerUid | "self"> <targetUid> <verbName | "interaction" | "activation" | "alternative">

cmd-invokeverb-invalid-args = invokeverb takes 2 arguments.

cmd-invokeverb-invalid-player-uid = Player uid could not be parsed, or "self" was not passed.
cmd-invokeverb-invalid-target-uid = Target uid could not be parsed.

cmd-invokeverb-invalid-player-entity = Player uid given does not correspond to a valid entity.
cmd-invokeverb-invalid-target-entity = Target uid given does not correspond to a valid entity.

cmd-invokeverb-success = Invoked verb '{ $verb }' on { $target } with { $player } as the user.

cmd-invokeverb-verb-not-found = Could not find verb { $verb } on { $target }.
