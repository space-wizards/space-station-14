device-pda-slot-component-slot-name-cartridge = Картридж
default-program-name = Программа
notekeeper-program-name = Заметки
nano-task-program-name = NanoTask
news-read-program-name = Новости станции
crew-manifest-program-name = Манифест экипажа
crew-manifest-cartridge-loading = Загрузка...
net-probe-program-name = Зонд сетей
net-probe-scan = Просканирован { $device }!
net-probe-label-name = Название
net-probe-label-address = Адрес
net-probe-label-frequency = Частота
net-probe-label-network = Сеть
log-probe-program-name = Зонд логов
log-probe-scan = Загружены логи устройства { $device }!
log-probe-label-time = Время
log-probe-label-accessor = Использовано:
log-probe-label-number = #
log-probe-print-button = Print Logs
log-probe-printout-device = Scanned Device: { $name }
log-probe-printout-header = Latest logs:
log-probe-printout-entry = #{ $number } / { $time } / { $accessor }
astro-nav-program-name = АстроНав
med-tek-program-name = МедТек
# Wanted list cartridge
wanted-list-program-name = Список разыскиваемых
nano-task-ui-heading-high-priority-tasks =
    { $amount ->
        [zero] No High Priority Tasks
        [one] 1 High Priority Task
       *[other] { $amount } High Priority Tasks
    }
nano-task-ui-heading-medium-priority-tasks =
    { $amount ->
        [zero] No Medium Priority Tasks
        [one] 1 Medium Priority Task
       *[other] { $amount } Medium Priority Tasks
    }
nano-task-ui-heading-low-priority-tasks =
    { $amount ->
        [zero] No Low Priority Tasks
        [one] 1 Low Priority Task
       *[other] { $amount } Low Priority Tasks
    }
nano-task-ui-done = Done
nano-task-ui-revert-done = Undo
nano-task-ui-priority-low = Low
nano-task-ui-priority-medium = Medium
nano-task-ui-priority-high = High
nano-task-ui-cancel = Cancel
nano-task-ui-print = Print
nano-task-ui-delete = Delete
nano-task-ui-save = Save
nano-task-ui-new-task = New Task
nano-task-ui-description-label = Description:
nano-task-ui-description-placeholder = Get something important
nano-task-ui-requester-label = Requester:
nano-task-ui-requester-placeholder = John Nanotrasen
nano-task-ui-item-title = Edit Task
nano-task-printed-description = Description: { $description }
nano-task-printed-requester = Requester: { $requester }
nano-task-printed-high-priority = Priority: High
nano-task-printed-medium-priority = Priority: Medium
nano-task-printed-low-priority = Priority: Low
wanted-list-label-no-records = Всё спокойно, ковбой.
wanted-list-search-placeholder = Поиск по имени и статусу
wanted-list-age-label = [color=darkgray]Возраст:[/color] [color=white]{ $age }[/color]
wanted-list-job-label = [color=darkgray]Должность:[/color] [color=white]{ $job }[/color]
wanted-list-species-label = [color=darkgray]Раса:[/color] [color=white]{ $species }[/color]
wanted-list-gender-label = [color=darkgray]Гендер:[/color] [color=white]{ $gender }[/color]
wanted-list-reason-label = [color=darkgray]Причина:[/color] [color=white]{ $reason }[/color]
wanted-list-unknown-reason-label = неизвестная причина
wanted-list-initiator-label = [color=darkgray]Инициатор:[/color] [color=white]{ $initiator }[/color]
wanted-list-unknown-initiator-label = неизвестный инициатор
wanted-list-status-label = [color=darkgray]статус:[/color] { $status ->
        [suspected] [color=yellow]подозревается[/color]
        [wanted] [color=red]разыскивается[/color]
        [detained] [color=#b18644]под арестом[/color]
        [discharged] [color=green]освобождён[/color]
       *[other] нет
    }
wanted-list-history-table-time-col = Время
wanted-list-history-table-reason-col = Преступление
wanted-list-history-table-initiator-col = Инициатор
