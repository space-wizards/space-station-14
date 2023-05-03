uploadfolder-command-description = Рекурсивно загружает папку из вашей папки UserData в contentDB сервера.
uploadfolder-command-help = uploadfolder [папка, которую вы хотите загрузить в userdata/UploadFolder]
uploadfolder-command-wrong-args = Неверное число аргументов!
uploadfolder-command-folder-not-found = Папка { $folder } не найдена!
uploadfolder-command-resource-upload-disabled = Network Resource Uploading в настоящее время отключена. Проверьте CVar-ы сервера.
uploadfolder-command-file-too-big = Файл { $filename } превышает текущие ограничения размера! Он должен быть меньше { $sizeLimit } MB. Пропуск.
uploadfolder-command-success =
    Загружено: { $fileCount } { $fileCount ->
        [one] файл
        [few] файла
       *[other] файлов
    }.
