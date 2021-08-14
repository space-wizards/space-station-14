## CreateVoteCommand

create-vote-command-description = Создает голосование
create-vote-command-help = Использование: createvote <'restart'|'preset'>
create-vote-command-cannot-call-vote-now = Вы не можете объявить голосование прямо сейчас!
create-vote-command-invalid-vote-type = Вы не можете объявить голосование прямо сейчас!

## CreateCustomCommand

create-custom-command-description = Создает пользовательское голосование
create-custom-command-help = customvote <title> <option1> <option2> [option3...]
create-custom-command-on-finished-tie = Ничья между {$ties}!
create-custom-command-on-finished-win = {$winner} победил!

## VoteCommand

vote-command-description = Голоса на активном голосовании
vote-command-help = vote <voteId> <option>
vote-command-cannot-call-vote-now = Вы не можете объявить голосование прямо сейчас!
vote-command-on-execute-error-must-be-player = Должен быть игроком
vote-command-on-execute-error-invalid-vote-id = Неверный ID голоса
vote-command-on-execute-error-invalid-vote-options = Неверные варианты голосования
vote-command-on-execute-error-invalid-vote = Неверное голосование
vote-command-on-execute-error-invalid-option = Неверный вариант

## ListVotesCommand

list-votes-command-description = Перечисляет активные в настоящее время голосования
list-votes-command-help = Использование: listvotes

## CancelVoteCommand

cancel-vote-command-description = Отменяет активное голосование
cancel-vote-command-help = Использование: cancelvote <id>
                           ID можно получить из команды listvotes.
cancel-vote-command-on-execute-error-invalid-vote-id = Неверное ID голосования
cancel-vote-command-on-execute-error-missing-vote-id = ID отсутствует
