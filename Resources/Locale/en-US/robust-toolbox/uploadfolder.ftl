uploadfolder-command-description = Загружает папку из вашей папки UserData рекурсивно в серверный contentDB.
uploadfolder-command-help = uploadfolder [папка в userdata/UploadFolder, которую вы хотите загрузить]
uploadfolder-command-wrong-args = Неправильное количество аргументов!
uploadfolder-command-folder-not-found = Папка { $folder } не найдена!
uploadfolder-command-resource-upload-disabled = Загрузка сетевых ресурсов сейчас отключена. Проверьте серверные CVar-ы.
uploadfolder-command-file-too-big = Файл { $filename } превышает лимит размера! Должно быть меньше { $sizeLimit } МБ. Пропуск.
uploadfolder-command-success = {$fileCount ->
    [one] Загружен 1 файл
    [few] Загружено { $fileCount } файла
    *[other] Загружено { $fileCount } файлов
}.
