### Voting system related console commands


## 'createvote' command

cmd-createvote-desc = Создаёт голосование
cmd-createvote-help = Использование: createvote <'restart'|'preset'|'map'>
cmd-createvote-cannot-call-vote-now = Сейчас вы не можете запустить голосование!
cmd-createvote-invalid-vote-type = Неверный тип голосования
cmd-createvote-arg-vote-type = <vote type>

## 'customvote' command

cmd-customvote-desc = Создаёт настраиваемое голосование
cmd-customvote-help = Использование: customvote <title> <option1> <option2> [option3...]
cmd-customvote-on-finished-tie = Ничья между { $ties }!
cmd-customvote-on-finished-win = { $winner } побеждает!
cmd-customvote-arg-title = <title>
cmd-customvote-arg-option-n = <option{ $n }>

## 'vote' command

cmd-vote-desc = Голосует в активном голосовании
cmd-vote-help = vote <voteId> <option>
cmd-vote-cannot-call-vote-now = Сейчас вы не можете запустить голосование!
cmd-vote-on-execute-error-must-be-player = Должен быть игроком
cmd-vote-on-execute-error-invalid-vote-id = Неверное ID голосования
cmd-vote-on-execute-error-invalid-vote-options = Неверные параметры голосования
cmd-vote-on-execute-error-invalid-vote = Неверное голосование
cmd-vote-on-execute-error-invalid-option = Неверный параметр

## 'listvotes' command

cmd-listvotes-desc = Перечисляет активные голосования
cmd-listvotes-help = Использование: listvotes

## 'cancelvote' command

cmd-cancelvote-desc = Отменяет текущее голосование
cmd-cancelvote-help =
    Использование: cancelvote <id>
    Вы можете найти ID с помощью команды listvotes.
cmd-cancelvote-error-invalid-vote-id = Неверный ID голосования
cmd-cancelvote-error-missing-vote-id = Отсутствует ID
cmd-cancelvote-arg-id = <id>
