# ban
cmd-ban-desc = Банит кого-либо
cmd-ban-help = Использование: ban <name or user ID> <reason> [продолжительность в минутах, без указания или 0 для пермабана]
cmd-ban-player = Не удалось найти игрока с таким именем.
cmd-ban-self = Вы не можете забанить себя!
cmd-ban-hint = <name/user ID>
cmd-ban-hint-reason = <reason>
cmd-ban-hint-duration = [продолжительность]

cmd-ban-hint-duration-1 = Навсегда
cmd-ban-hint-duration-2 = 1 день
cmd-ban-hint-duration-3 = 3 дня
cmd-ban-hint-duration-4 = 1 неделя
cmd-ban-hint-duration-5 = 2 недели
cmd-ban-hint-duration-6 = 1 месяц

# listbans
cmd-banlist-desc = Список активных банов пользователя.
cmd-banlist-help = Использование: banlist <name or user ID>
cmd-banlist-empty = Нет активных банов у пользователя { $user }
cmd-banlistF-hint = <name/user ID>

cmd-ban_exemption_update-desc = Установить исключение для типа бана игрока.
cmd-ban_exemption_update-help = Использовать: ban_exemption_update <player> <flag> [<flag> [...]]
    Укажите несколько флагов, чтобы дать игроку несколько флагов освобождения от бана.
    Чтобы удалить все исключения, запустите эту команду и установите "None" в качестве единственного флага.

cmd-ban_exemption_update-nargs = Ожидается как минимум 2 аргумента
cmd-ban_exemption_update-locate = Не удалось найти игрока '{$player}'.
cmd-ban_exemption_update-invalid-flag = Неверный флаг '{$flag}'.
cmd-ban_exemption_update-success = Обновлены флаги исключения из бана для '{$player}' ({$uid}).
cmd-ban_exemption_update-arg-player = <player>
cmd-ban_exemption_update-arg-flag = <flag>

cmd-ban_exemption_get-desc = Показать исключения из бана для определенного игрока.
cmd-ban_exemption_get-help = Использовать: ban_exemption_get <player>

cmd-ban_exemption_get-nargs = Ожидается ровно 1 аргумент
cmd-ban_exemption_get-none = Пользователь не освобождается от каких-либо банов.
cmd-ban_exemption_get-show = Пользователь освобожден от следующих флагов бана: {$flags}.
cmd-ban_exemption_get-arg-player = <player>
