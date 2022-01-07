## CreateVoteCommand

create-vote-command-description = Создает голосование
create-vote-command-help = Использование: createvote <'restart'|'preset'>
create-vote-command-cannot-call-vote-now = Сейчас вы не можете запустить голосование!
create-vote-command-invalid-vote-type = Сейчас вы не можете запустить голосование!

## CreateCustomCommand

create-custom-command-description = Создаёт настраиваемое голосование
create-custom-command-help = customvote <title> <option1> <option2> [option3...]
create-custom-command-on-finished-tie = Ничья между { $ties }!
create-custom-command-on-finished-win = { $winner } побеждает!

## VoteCommand

vote-command-description = Голосует в активном голосовании
vote-command-help = vote <voteId> <option>
vote-command-cannot-call-vote-now = Сейчас вы не можете запустить голосование!
vote-command-on-execute-error-must-be-player = Должен быть игроком
vote-command-on-execute-error-invalid-vote-id = Неверное ID голосования
vote-command-on-execute-error-invalid-vote-options = Неверные параметры голосования
vote-command-on-execute-error-invalid-vote = Неверное голосование
vote-command-on-execute-error-invalid-option = Неверный параметр

## ListVotesCommand

list-votes-command-description = Перечисляет активные голосования
list-votes-command-help = Использование: listvotes

## CancelVoteCommand

cancel-vote-command-description = Отменяет текущее голосование
cancel-vote-command-help =
    Использование: cancelvote <id>
    Вы можете найти ID с помощью команды listvotes.
cancel-vote-command-on-execute-error-invalid-vote-id = Неверный ID голосования
cancel-vote-command-on-execute-error-missing-vote-id = Отсутствует ID
