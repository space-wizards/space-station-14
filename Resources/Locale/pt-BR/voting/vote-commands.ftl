### Voting system related console commands

## 'createvote' command

cmd-createvote-desc = Cria uma votação
cmd-createvote-help = Uso: createvote <'restart'|'preset'|'map'>
cmd-createvote-cannot-call-vote-now = Você não pode criar uma votação agora!
cmd-createvote-invalid-vote-type = Tipo de votação inválida
cmd-createvote-arg-vote-type = <vote type>

## 'customvote' command

cmd-customvote-desc = Cria uma votação customizada
cmd-customvote-help = Uso: customvote <title> <option1> <option2> [option3...]
cmd-customvote-on-finished-tie = Empate entre {$ties}!
cmd-customvote-on-finished-win = {$winner} venceu!
cmd-customvote-arg-title = <title>
cmd-customvote-arg-option-n = <option{ $n }>

## 'vote' command

cmd-vote-desc = Vota em uma votação ativa
cmd-vote-help = vote <voteId> <option>
cmd-vote-cannot-call-vote-now = Você não pode votar agora!
cmd-vote-on-execute-error-must-be-player = Você deve ser um jogador
cmd-vote-on-execute-error-invalid-vote-id = ID de voto inválido
cmd-vote-on-execute-error-invalid-vote-options = Opções de voto inválido
cmd-vote-on-execute-error-invalid-vote = Voto inválido
cmd-vote-on-execute-error-invalid-option = Opção inválida

## 'listvotes' command

cmd-listvotes-desc = Lista as votações ativas
cmd-listvotes-help = Uso: listvotes

## 'cancelvote' command

cmd-cancelvote-desc = Cancela uma votação ativa
cmd-cancelvote-help = Uso: cancelvote <id>
                      Você pode pegar o ID do comando listvotes.
cmd-cancelvote-error-invalid-vote-id = ID de voto inválido
cmd-cancelvote-error-missing-vote-id = ID faltando
cmd-cancelvote-arg-id = <id>
