import os
import yaml
import re
from datetime import datetime
from github import Github

print("Environment Variables:")
changelog_path = os.getenv("CHANGELOG_FILE_PATH")
pr_number = os.getenv("PR_NUMBER")
repo_name = os.getenv("GITHUB_REPOSITORY")
github_token = os.getenv("GITHUB_TOKEN")
print(f"CHANGELOG_FILE_PATH: {changelog_path}")
print(f"PR_NUMBER: {pr_number}")
print(f"GITHUB_REPOSITORY: {repo_name}")
print(f"GITHUB_TOKEN is set: {bool(github_token)}")

g = Github(github_token)
repo = g.get_repo(repo_name)
pr = repo.get_pull(int(pr_number))

def parse_changelog(pr_body):
    changelog_entries = []
    pattern = r":cl: ([^\n]+)\n- (add|remove|tweak|fix): ([^\n]+)"
    matches = list(re.finditer(pattern, pr_body, re.MULTILINE))
    print(f"Found {len(matches)} matches with pattern.")

    for match in matches:
        author = match.group(1).strip()
        change_type = match.group(2).capitalize()  
        message = match.group(3).strip()  

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
    if not pr.body:
        print("PR body is empty.")
        return

    print("PR Body:", repr(pr.body))

    if ":cl:" in pr.body:
        print("Found ':cl:' in PR body.")
        merge_time = pr.merged_at
        entries = parse_changelog(pr.body)

        print("Parsed entries:", entries)

        if not entries:
            print("No changelog entries found after parsing.")
            return

        if os.path.exists(changelog_path):
            print(f"Changelog file exists at {changelog_path}")
            with open(changelog_path, "r", encoding='utf-8') as file:
                changelog_data = yaml.safe_load(file) or {"Entries": []}
        else:
            print(f"Changelog file does not exist and will be created at {changelog_path}")
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
                "time": merge_time.isoformat(timespec='microseconds'),
                "url": f"https://github.com/{repo_name}/pull/{pr_number}"
            }
            changelog_data["Entries"].append(changelog_entry)

        os.makedirs(os.path.dirname(changelog_path), exist_ok=True)

        with open(changelog_path, "w", encoding='utf-8') as file:
            yaml.dump(changelog_data, file, allow_unicode=True, explicit_start=True)
            file.write('\n')
        print(f"Changelog updated and written to {changelog_path}")
    else:
        print("No ':cl:' tag found in PR body.")
        return

if __name__ == "__main__":
    update_changelog()