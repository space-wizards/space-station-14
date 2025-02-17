# Shell description
accept-ert-command-description = Принимает запрос на спавн ОБР. Не одобренные запросы на вызов отрядов помимо "Эмбер" будут автоматически отклонены.
accept-ert-command-help-text = Использование: { $command }
fake-accept-ert-command-description = Отменяет спавн ОБР с сообщением о якобы успешном спавне. Используйте если вы хотите заспавнить свой ОБР.
fake-accept-ert-command-help-text = Использование: { $command }
refuse-ert-command-description = Досрочно отказывает в запросе на спавн любого ОБР.
refuse-ert-command-help-text = Использование: { $command }
# Shell errors
ert-command-invalid-grid = Вы должны находиться на сетке станции, статус вызова ОБР которой собираетесь изменить
ert-command-no-component = Не найден компонент ERTCallComponent
ert-command-no-ert-called = ОБР не вызван
ert-command-no-ert-called-in-component = Не найдена команда ОБР для одобрения запроса в компоненте ERTCallComponent
# Admin alerts
ert-command-admin-alert-accept = { $admin } одобрил вызов { $ert }. Игроки узнают о одобрении запроса по завершении фазы одобрения.
ert-command-admin-alert-fake-accept = { $admin } ложно одобрил вызов { $ert }. ОБР не будет заспавнен автоматически.
ert-command-admin-alert-refuse = { $admin } отклонил вызов { $ert }.
comms-console-menu-ert-message-alert = Игрок: { $name } запросил вызов { $ert } по причине: { $message }
# Admin UI
admin-player-actions-window-ert = Запросы ОБР
admin-player-actions-window-accept = Одобрить запрос
admin-player-actions-window-fake-accept = Ложно одобрить запрос
admin-player-actions-window-refuse = Досрочный отказ
