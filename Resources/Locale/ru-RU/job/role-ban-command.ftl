### Localization for role ban command

cmd-roleban-desc = Запрещает пользователю играть на роли
cmd-roleban-help = Использование: roleban <name or user ID> <job> <reason> [продолжительность в минутах, не указывать или 0 для навсегда]

## Completion result hints

cmd-roleban-hint-1 = <name or user ID>
cmd-roleban-hint-2 = <job>
cmd-roleban-hint-3 = <reason>
cmd-roleban-hint-4 = [продолжительность в минутах, не указывать или 0 для навсегда]
cmd-roleban-hint-5 = [severity]
cmd-roleban-hint-duration-1 = Навсегда
cmd-roleban-hint-duration-2 = 1 день
cmd-roleban-hint-duration-3 = 3 дня
cmd-roleban-hint-duration-4 = 1 неделя
cmd-roleban-hint-duration-5 = 2 недели
cmd-roleban-hint-duration-6 = 1 месяц

### Localization for role unban command

cmd-roleunban-desc = Возвращает пользователю возможность играть на роли
cmd-roleunban-help = Использование: roleunban <role ban id>

## Completion result hints

cmd-roleunban-hint-1 = <role ban id>

### Localization for roleban list command

cmd-rolebanlist-desc = Список запретов ролей игрока
cmd-rolebanlist-help = Использование: <name or user ID> [include unbanned]

## Completion result hints

cmd-rolebanlist-hint-1 = <name or user ID>
cmd-rolebanlist-hint-2 = [include unbanned]
cmd-roleban-minutes-parse = { $time } - недопустимое количество минут.\n{ $help }
cmd-roleban-severity-parse = ${ severity } is not a valid severity\n{ $help }.
cmd-roleban-arg-count = Недопустимое количество аргументов.
cmd-roleban-job-parse = Работа { $job } не существует.
cmd-roleban-name-parse = Невозможно найти игрока с таким именем.
cmd-roleban-existing = { $target } уже имеет запрет на роль { $role }.
cmd-roleban-success = { $target } запрещено играть на роли { $role } по причине { $reason } { $length }.
cmd-roleban-inf = навсегда
cmd-roleban-until = до { $expires }
# Department bans
cmd-departmentban-desc = Запрещает пользователю играть на ролях, входящих в отдел
cmd-departmentban-help = Использование: departmentban <name or user ID> <department> <reason> [продолжительность в минутах, не указывать или 0 для навсегда]
