### Comandos relacionados con el sistema de votación

## Comando 'createvote'

cmd-createvote-desc = Crea una votación
cmd-createvote-help = Uso: createvote <'restart'|'preset'|'map'>
cmd-createvote-cannot-call-vote-now = ¡No puedes llamar a una votación ahora mismo!
cmd-createvote-invalid-vote-type = Tipo de votación inválido
cmd-createvote-arg-vote-type = <tipo de votación>

## Comando 'customvote'

cmd-customvote-desc = Crea una votación personalizada
cmd-customvote-help = Uso: customvote <title> <option1> <option2> [option3...]
cmd-customvote-on-finished-tie = ¡Empate entre {$ties}!
cmd-customvote-on-finished-win = ¡{$winner} gana!
cmd-customvote-arg-title = <title>
cmd-customvote-arg-option-n = <option{ $n }>

## Comando 'vote'

cmd-vote-desc = Vota en una votación activa
cmd-vote-help = vote <voteId> <option>
cmd-vote-cannot-call-vote-now = ¡No puedes llamar a una votación ahora mismo!
cmd-vote-on-execute-error-must-be-player = Debe ser un jugador
cmd-vote-on-execute-error-invalid-vote-id = ID de votación inválido
cmd-vote-on-execute-error-invalid-vote-options = Opciones de votación inválidas
cmd-vote-on-execute-error-invalid-vote = Votación inválida
cmd-vote-on-execute-error-invalid-option = Opción inválida

## Comando 'listvotes'

cmd-listvotes-desc = Lista las votaciones activas actuales
cmd-listvotes-help = Uso: listvotes

## Comando 'cancelvote'

cmd-cancelvote-desc = Cancela una votación activa
cmd-cancelvote-help = Uso: cancelvote <id>
                      Puedes obtener el ID del comando listvotes.
cmd-cancelvote-error-invalid-vote-id = ID de votación inválido
cmd-cancelvote-error-missing-vote-id = ID faltante
cmd-cancelvote-arg-id = <id>
