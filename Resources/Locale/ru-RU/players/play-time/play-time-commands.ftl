parse-minutes-fail = Не удалось спарсить '{ $minutes }' как минуты
parse-session-fail = Не найдена сессия для '{ $username }'

## Role Timer Commands

# - playtime_addoverall
cmd-playtime_addoverall-desc = Добавляет указанное число минут к общему игровому времени игрока
cmd-playtime_addoverall-help = Использование: { $command } <user name> <minutes>
cmd-playtime_addoverall-succeed = Общее игровое время { $username } увеличено на { TOSTRING($time, "dddd\\:hh\\:mm") }.
cmd-playtime_addoverall-arg-user = <user name>
cmd-playtime_addoverall-arg-minutes = <minutes>
cmd-playtime_addoverall-error-args = Ожидается ровно два аргумента
# - playtime_addrole
cmd-playtime_addrole-desc = Добавляет указанное число минут к времени игрока на определённой роли
cmd-playtime_addrole-help = Использование: { $command } <user name> <role> <minutes>
cmd-playtime_addrole-succeed = Игровое время для { $username } / \'{ $role }\' увеличено на { TOSTRING($time, "dddd\\:hh\\:mm") }.
cmd-playtime_addrole-arg-user = <user name>
cmd-playtime_addrole-arg-role = <role>
cmd-playtime_addrole-arg-minutes = <minutes>
cmd-playtime_addrole-error-args = Ожидается ровно три аргумента
# - playtime_getoverall
cmd-playtime_getoverall-desc = Получить общее игровое время игрока в минутах
cmd-playtime_getoverall-help = Использование: { $command } <user name>
cmd-playtime_getoverall-success = Общее игровое время { $username } составляет { TOSTRING($time, "dddd\\:hh\\:mm") }.
cmd-playtime_getoverall-arg-user = <user name>
cmd-playtime_getoverall-error-args = Ожидается ровно один аргумент
# - GetRoleTimer
cmd-playtime_getrole-desc = Получает все или один таймер роли от игрока
cmd-playtime_getrole-help = Использование: { $command } <user name> [role]
cmd-playtime_getrole-no = Таймеров ролей не найдено
cmd-playtime_getrole-role = Роль: { $role }, игровое время: { $time }
cmd-playtime_getrole-overall = Общее игровое время { $time }
cmd-playtime_getrole-succeed = Игровое время { $username } составляет: { TOSTRING($time, "dddd\\:hh\\:mm") }.
cmd-playtime_getrole-arg-user = <user name>
cmd-playtime_getrole-arg-role = <role|'Overall'>
cmd-playtime_getrole-error-args = Ожидается ровно один или два аргумента
# - playtime_save
cmd-playtime_save-desc = Сохранение игрового времени игрока в БД
cmd-playtime_save-help = Использование: { $command } <user name>
cmd-playtime_save-succeed = Игровое время { $username } сохранено
cmd-playtime_save-arg-user = <user name>
cmd-playtime_save-error-args = Ожидается ровно один аргумент

## 'playtime_flush' command'

cmd-playtime_flush-desc = Записывает активные трекеры в хранение отслеживании игрового времени.
cmd-playtime_flush-help =
    Использование: { $command } [user name]
    Это вызывает запись только во внутреннее хранилище, при это не записывая немедленно в БД.
    Если пользователь передан, то только этот пользователь будет обработан.
cmd-playtime_flush-error-args = Ожидается ноль или один аргумент
cmd-playtime_flush-arg-user = [user name]
