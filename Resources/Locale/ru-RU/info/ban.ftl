# ban
cmd-ban-desc = Банит кого-либо
cmd-ban-help = Использование: ban <name or user ID> <reason> [продолжительность в минутах, без указания или 0 для пермабана]
cmd-ban-player = Не удалось найти игрока с таким именем.
cmd-ban-invalid-minutes = ${ minutes } не является корректным значеним для минут!
cmd-ban-invalid-severity = ${ severity } не является корректным значением строгости!
cmd-ban-invalid-arguments = Недостаточно аргументов.
cmd-ban-hint = <name/user ID>
cmd-ban-hint-reason = <причина>
cmd-ban-hint-severity = [строгость]
cmd-ban-hint-duration = [продолжительность]
cmd-ban-hint-duration-1 = Навсегда
cmd-ban-hint-duration-2 = 1 день
cmd-ban-hint-duration-3 = 3 дня
cmd-ban-hint-duration-4 = 1 неделя
cmd-ban-hint-duration-5 = 2 недели
# ban panel
cmd-banpanel-desc = Opens the ban panel
cmd-banpanel-help = Usage: banpanel [name or user guid]
cmd-banpanel-server = This can not be used from the server console
cmd-banpanel-player-err = The specified player could not be found
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
# Ban panel
ban-panel-title = Панель блокировки
ban-panel-player = Игрок
ban-panel-ip = IP
ban-panel-hwid = HWID
ban-panel-reason = Причина
ban-panel-last-conn = Использовать IP и HWID с момента последнего подключения?
ban-panel-submit = Забанить
ban-panel-confirm = Вы уверены?
ban-panel-tabs-basic = Основная информация
ban-panel-tabs-reason = Причина
ban-panel-tabs-players = Список игроков
ban-panel-tabs-role = Список блокировок
ban-panel-no-data = Вы должны указать пользователя, IP или HWID
ban-panel-invalid-ip = IP адрес указан некорректно.
ban-panel-select = Выберите тип
ban-panel-server = Серверная блокировка
ban-panel-role = Блокировка роли
ban-panel-minutes = Минут
ban-panel-hours = Часов
ban-panel-days = Дней
ban-panel-weeks = Недель
ban-panel-months = Месяцев
ban-panel-years = Лет
ban-panel-permanent = Навсегда
ban-panel-ip-hwid-tooltip = Оставьте пустым и поставьте галочку, чтобы использовать данные последнего подключения
ban-panel-severity = Тяжесть:
ban-panel-erase = Очистить следы игрока из чата и раунда
# Ban string
server-ban-string = { $admin } выдал серверную блокировку { $severity } тяжести, истекающую { $expires }. Игроку [{ $name }, { $ip }, { $hwid }] по причине: { $reason }
server-ban-string-never = никогда
server-ban-string-no-pii = { $admin } выдал серверную блокировку { $severity } тяжести, истекающую { $expires }. Игроку { $name } по причине: { $reason }
cmd-ban_exemption_get-arg-player = <player>
