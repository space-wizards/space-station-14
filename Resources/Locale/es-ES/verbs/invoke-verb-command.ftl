### Localización utilizada para el comando de verbo de invocación.
# Principalmente mensajes de ayuda + error.

invoke-verb-command-description = Invoca un verbo con el nombre dado en una entidad, con la entidad del jugador
invoke-verb-command-help = invokeverb <playerUid | "self"> <targetUid> <verbName | "interaction" | "activation" | "alternative">

invoke-verb-command-invalid-args = invokeverb requiere 2 argumentos.

invoke-verb-command-invalid-player-uid = El uid del jugador no pudo ser analizado, o "self" no fue pasado.
invoke-verb-command-invalid-target-uid = El uid del objetivo no pudo ser analizado.

invoke-verb-command-invalid-player-entity = El uid del jugador dado no corresponde a una entidad válida.
invoke-verb-command-invalid-target-entity = El uid del objetivo dado no corresponde a una entidad válida.

invoke-verb-command-success = Verbo '{ $verb }' invocado en { $target } con { $player } como el usuario.

invoke-verb-command-verb-not-found = No se pudo encontrar el verbo { $verb } en { $target }.