import os
import yaml
import re
from datetime import datetime
from github import Github

# Параметры окружения
changelog_path = os.getenv("CHANGELOG_FILE_PATH")
pr_number = os.getenv("PR_NUMBER")
repo_name = os.getenv("GITHUB_REPOSITORY")
github_token = os.getenv("GITHUB_TOKEN")

# Инициализация GitHub API
g = Github(github_token)
repo = g.get_repo(repo_name)
pr = repo.get_pull(int(pr_number))

def parse_changelog(pr_body):
    changelog_entries = []
    # Обновленное регулярное выражение с точной привязкой к типу и сообщению об изменении
    pattern = r":cl: ([^\n]+)\n- (add|remove|tweak|fix): ([^\n]+)"
    matches = re.finditer(pattern, pr_body, re.MULTILINE)

    for match in matches:
        author = match.group(1).strip()
        change_type = match.group(2).capitalize()  # Получаем тип изменения
        message = match.group(3).strip()  # Получаем само сообщение

        # Добавляем данные в список изменений
        changelog_entries.append({
            "author": author,
            "type": change_type,
            "message": message
        })
    return changelog_entries

def get_last_id(changelog_data):
    if not changelog_data or "Entries" not in changelog_data or not changelog_data["Entries"]:
        return 0
    return max(entry["id"] for entry in changelog_data["Entries"])

def update_changelog():
    # Выведем тело PR для отладки
    print("PR Body:", pr.body)
    
    if ":cl:" in pr.body:
        merge_time = pr.merged_at
        entries = parse_changelog(pr.body)
        
        # Отладочная печать для проверки парсинга
        print("Parsed entries:", entries)
        
        if not entries:
            print("No changelog entries found.")
            return
        
        if os.path.exists(changelog_path):
            with open(changelog_path, "r") as file:
                changelog_data = yaml.safe_load(file) or {"Entries": []}
        else:
            changelog_data = {"Entries": []}

        last_id = get_last_id(changelog_data)
        for entry in entries:
            last_id += 1
            changelog_entry = {
                "author": entry["author"],
                "changes": [{
                    "message": entry["message"],
                    "type": entry["type"]
                }],
                "id": last_id,
                "time": merge_time.isoformat()
            }
            changelog_data["Entries"].append(changelog_entry)

        # Запись с добавлением новой строки в конце
        with open(changelog_path, "w") as file:
            yaml.dump(changelog_data, file, allow_unicode=True, explicit_start=True)
            file.write('\n')  # Добавить новую строку в конце файла

if __name__ == "__main__":
    update_changelog()
