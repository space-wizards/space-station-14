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
cmd-ban_exemption_update-desc = Установить исключение на типы банов игрока.
cmd-ban_exemption_update-help =
    Использование: ban_exemption_update <player> <flag> [<flag> [...]]
    Укажите несколько флагов, чтобы дать игроку исключение из нескольких типов банов.
    Чтобы удалить все исключения, выполните эту команду и укажите единственным флагом "None".
cmd-ban_exemption_update-nargs = Ожидалось хотя бы 2 аргумента
cmd-ban_exemption_update-locate = Не удалось найти игрока '{ $player }'.
cmd-ban_exemption_update-invalid-flag = Недопустимый флаг '{ $flag }'.
cmd-ban_exemption_update-success = Обновлены флаги исключений банов для '{ $player }' ({ $uid }).
cmd-ban_exemption_update-arg-player = <player>
cmd-ban_exemption_update-arg-flag = <flag>
cmd-ban_exemption_get-desc = Показать исключения банов для определённого игрока.
cmd-ban_exemption_get-help = Использование: ban_exemption_get <player>
cmd-ban_exemption_get-nargs = Ожидался ровно 1 аргумент
cmd-ban_exemption_get-none = Пользователь не имеет исключений от банов.
cmd-ban_exemption_get-show = Пользователь исключён из банов со следующими флагами: { $flags }.
cmd-ban_exemption_get-arg-player = <player>
