### for technical and/or system messages


## General

shell-command-success = Команда выполнена.
shell-invalid-command = Неверная команда.
shell-invalid-command-specific = Неверная команда { $commandName }.
shell-cannot-run-command-from-server = Вы не можете выполнить эту команду с сервера.
shell-only-players-can-run-this-command = Только игроки могут выполнять эту команду.
shell-must-be-attached-to-entity = Для выполнения этой команды вы должны быть прикреплены к сущности.

## Arguments

shell-need-exactly-one-argument = Нужен ровно один аргумент.
shell-wrong-arguments-number-need-specific =
    Нужно { $properAmount } { $properAmount ->
        [one] аргумент
        [few] аргумента
       *[other] аргументов
    }, было { $currentAmount } { $currentAmount ->
        [one] аргумент
        [few] аргумента
       *[other] аргументов
    }.
shell-argument-must-be-number = Аргумент должен быть числом.
shell-argument-must-be-boolean = Аргумент должен быть boolean.
shell-wrong-arguments-number = Неправильное количество аргументов.
shell-need-between-arguments = Нужно от { $lower } до { $upper } аргументов!
shell-need-minimum-arguments = Нужно не менее { $minimum } аргументов!
shell-need-minimum-one-argument = Нужен хотя бы один аргумент!
shell-argument-uid = EntityUid

## Guards

shell-entity-is-not-mob = Целевая сущность не является мобом!
shell-invalid-entity-id = Недопустимый ID сущности.
shell-invalid-grid-id = Недопустимый ID сетки.
shell-invalid-map-id = Недопустимый ID карты.
shell-invalid-entity-uid = { $uid } не является допустимым идентификатором uid.
shell-invalid-bool = Неверный boolean.
shell-entity-uid-must-be-number = EntityUid должен быть числом.
shell-could-not-find-entity = Не удалось найти сущность { $entity }.
shell-could-not-find-entity-with-uid = Не удалось найти сущность с uid { $uid }.
shell-entity-with-uid-lacks-component = Сущность с uid { $uid } не имеет компонента { $componentName }.
shell-invalid-color-hex = Недопустимый HEX-цвет!
shell-target-player-does-not-exist = Целевой игрок не существует!
shell-target-entity-does-not-have-message = Целевая сущность не имеет { $missing }!
shell-timespan-minutes-must-be-correct = { $span } не является допустимым промежутком времени в минутах.
shell-argument-must-be-prototype = Аргумент { $index } должен быть ${ prototypeName }!
shell-argument-number-must-be-between = Аргумент { $index } должен быть числом от { $lower } до { $upper }!
shell-argument-station-id-invalid = Аргумент { $index } должен быть валидным station id!
shell-argument-map-id-invalid = Аргумент { $index } должен быть валидным map id!
shell-argument-number-invalid = Аргумент { $index } должен быть валидным числом!
# Hints
shell-argument-username-hint = <username>
shell-argument-username-optional-hint = [username]
