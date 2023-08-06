# ban
cmd-ban-desc = Банит кого-либо
cmd-ban-help = Использование: ban <name or user ID> <reason> [продолжительность в минутах, без указания или 0 для пермабана]
cmd-ban-player = Не удалось найти игрока с таким именем.
cmd-ban-invalid-minutes = ${ minutes } is not a valid amount of minutes!
cmd-ban-invalid-severity = ${ severity } is not a valid severity!
cmd-ban-invalid-arguments = Invalid amount of arguments
cmd-ban-hint = <name/user ID>
cmd-ban-hint-reason = <reason>
cmd-ban-hint-severity = [severity]
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
ban-panel-title = Banning panel
ban-panel-player = Player
ban-panel-ip = IP
ban-panel-hwid = HWID
ban-panel-reason = Reason
ban-panel-last-conn = Use IP and HWID from last connection?
ban-panel-submit = Ban
ban-panel-confirm = Are you sure?
ban-panel-tabs-basic = Basic info
ban-panel-tabs-reason = Reason
ban-panel-tabs-players = Player List
ban-panel-tabs-role = Role ban info
ban-panel-no-data = You must provide either a user, IP or HWID to ban
ban-panel-invalid-ip = The IP address could not be parsed. Please try again
ban-panel-select = Select type
ban-panel-server = Server ban
ban-panel-role = Role ban
ban-panel-minutes = Minutes
ban-panel-hours = Hours
ban-panel-days = Days
ban-panel-weeks = Weeks
ban-panel-months = Months
ban-panel-years = Years
ban-panel-permanent = Permanent
ban-panel-ip-hwid-tooltip = Leave empty and check the checkbox below to use last connection's details
ban-panel-severity = Severity:
# Ban string
server-ban-string = { $admin } created a { $severity } severity server ban that expires { $expires } for [{ $name }, { $ip }, { $hwid }], with reason: { $reason }
server-ban-string-never = never
server-ban-string-no-pii = { $admin } created a { $severity } severity server ban that expires { $expires } for { $name } with reason: { $reason }
cmd-ban_exemption_get-arg-player = <player>
