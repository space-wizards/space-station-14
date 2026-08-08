# Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

personnel-records-console-permission-denied = Доступ запрещён.
personnel-records-console-unknown-officer = <неизвестный>
personnel-records-console-job-assignment-blocked = Место по этой должности уже занято. Требуется доступ Центрального Командования.

## Печать приказа (PersonnelPrintingSystem)

paperwork-form-title-personnel-discipline = Приказ о дисциплинарном взыскании
personnel-records-print-sanction-reprimand = выговора
personnel-records-print-sanction-demotion = понижения
personnel-records-print-sanction-dismissal = увольнения
personnel-records-print-unknown-department = неизвестного отдела

## Радиооповещения — отдел цели

personnel-records-console-announce-reprimand = Сотруднику { $name } ({ $job }) назначен выговор. Причина: { $reason }. Ответственный: { $officer }.
personnel-records-console-announce-demotion = Сотрудник { $name } ({ $job }) назначен на понижение. Причина: { $reason }. Ответственный: { $officer }.
personnel-records-console-announce-dismissal = Сотрудник { $name } ({ $job }) назначен на увольнение. Причина: { $reason }. Ответственный: { $officer }.
personnel-records-console-announce-annul = Приказ в отношении { $name } ({ $job }) отменён. Сопровождение не требуется. Причина: { $reason }. Ответственный: { $officer }.

## Радиооповещения — канал СБ

personnel-records-console-announce-security-demotion = Сотрудник { $name } ({ $job }) назначен на понижение. Сопроводите к главе персонала. Ответственный: { $officer }.
personnel-records-console-announce-security-dismissal = Сотрудник { $name } ({ $job }) назначен на увольнение. Сопроводите к главе персонала. Ответственный: { $officer }.
personnel-records-console-announce-annul-security = Приказ в отношении { $name } ({ $job }) отменён. Сопровождение не требуется. Причина: { $reason }. Ответственный: { $officer }.

## Радиооповещения — исполнение приказа (PersonnelOrderCompletionSystem, без ответственного)

personnel-records-console-announce-executed = Приказ в отношении { $name } исполнен. Сопровождение не требуется.
personnel-records-console-announce-executed-security = Приказ в отношении { $name } исполнен. Сопровождение не требуется.

## Строки в историю криминальных записей (PersonnelSecurityBridgeSystem)

personnel-records-criminal-history-demotion = Кадровая служба: назначено понижение. Причина: { $reason }.
personnel-records-criminal-history-dismissal = Кадровая служба: назначено увольнение. Причина: { $reason }.
personnel-records-criminal-history-annulled = Кадровая служба: приказ отменён. Причина: { $reason }.
personnel-records-criminal-history-executed = Кадровая служба: приказ исполнен.

## Окно консоли

personnel-records-console-window-title = Консоль кадрового учёта
personnel-records-console-records-list-title = Члены экипажа
personnel-records-console-select-record-info = Выбрать запись.
personnel-records-console-no-records = Записи не найдены!
personnel-records-console-no-department = Отдел не определён.
personnel-records-console-show-all = Все

## Кадровый статус

personnel-records-console-status = Кадровый статус
personnel-records-status-none = Нет взысканий
personnel-records-status-reprimand = Выговор
personnel-records-status-demotion = Понижение
personnel-records-status-dismissal = Увольнение

personnel-records-console-reason-label = [color=gray]Причина[/color]
personnel-records-console-initiator-label = [color=gray]Ответственный[/color]
personnel-records-console-criminal-status = Криминальный статус

## Кнопки

personnel-records-console-reprimand-button = Выговор
personnel-records-console-demote-button = Понижение
personnel-records-console-dismiss-button = Увольнение
personnel-records-console-annul-button = Отменить приказ
personnel-records-console-print-button = Распечатать приказ
personnel-records-console-declare-wanted-button = Объявить в розыск
personnel-records-console-history-button = История взысканий

## Диалоги причины

personnel-records-console-reason = Причина
personnel-records-console-reason-placeholder = Опишите причину взыскания
personnel-records-console-annul-reason-placeholder = Опишите причину отмены приказа
personnel-records-console-wanted-reason-placeholder = Опишите причину розыска

## Карточка сотрудника

personnel-records-console-record-department = Отдел: { $department }

## Фильтры

personnel-records-filter-placeholder = Введите текст и нажмите "Enter"
personnel-records-name-filter = Имя
personnel-records-prints-filter = Отпечатки пальцев
personnel-records-dna-filter = ДНК
personnel-records-job-filter = Должность
personnel-records-species-filter = Раса

## Окно истории

personnel-records-history-window-title = История взысканий
personnel-records-no-history = История взысканий данного члена экипажа чиста.
personnel-records-history-type-reprimand = Выговор
personnel-records-history-type-demotion = Понижение
personnel-records-history-type-dismissal = Увольнение
personnel-records-history-type-annul = Отмена приказа
personnel-records-history-type-executed = Приказ исполнен
personnel-records-history-auto-executed = Приказ исполнен автоматически: должность изменена на «{ $job }».

## Кнопка «Уволить» на консоли ID-карт (PersonnelDismissalSystem)

personnel-dismissal-button = Уволить
personnel-dismissal-confirm = Вы уверены?
personnel-dismissal-permission-denied = Доступ запрещён.
personnel-dismissal-no-record = У карты нет кадровой записи.
personnel-dismissal-not-issued = Приказ об увольнении не выдан.
