## Strings for the "grant_connect_bypass" command.

cmd-grant_connect_bypass-desc = Временно разрешить пользователю обойти обычные проверки подключения.
cmd-grant_connect_bypass-help =
    Использование: grant_connect_bypass <пользователь> [длительность в минутах]
    Временно предоставляет пользователю возможность обойти обычные ограничения на подключение.
    Обход применяется только к этому игровому серверу и истекает через (по умолчанию) 1 час.
    Пользователь сможет подключиться, независимо от белого списка, паники или ограничения на количество игроков.
cmd-grant_connect_bypass-arg-user = <пользователь>
cmd-grant_connect_bypass-arg-duration = [длительность в минутах]
cmd-grant_connect_bypass-invalid-args = Ожидалось 1 или 2 аргумента
cmd-grant_connect_bypass-unknown-user = Не удалось найти пользователя '{ $user }'
cmd-grant_connect_bypass-invalid-duration = Неверная длительность '{ $duration }'
cmd-grant_connect_bypass-success = Успешно добавлено разрешение на обход для пользователя '{ $user }'
