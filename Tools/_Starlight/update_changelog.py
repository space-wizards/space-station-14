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

def remove_comments(pr_body):
    """
    Removes all content inside HTML comments <!-- --> from the PR body.
    """
    pattern = r"<!--.*?-->"
    cleaned_body = re.sub(pattern, "", pr_body, flags=re.DOTALL)
    return cleaned_body

def parse_changelog(pr_body):
    changelog_entries = []
    pattern = r"(?<!<!--\s)^:cl:\s+([^\n]+)\n((?:- (add|remove|tweak|fix): [^\n]+\n?)+)"
    matches = list(re.finditer(pattern, pr_body, re.MULTILINE))
    print(f"Found {len(matches)} ':cl:' blocks in PR body.")

    for match in matches:
        author = match.group(1).strip()
        changes_block = match.group(2).strip()
        changes = changes_block.splitlines()
        for change in changes:
            change_pattern = r"-\s+(add|remove|tweak|fix):\s+(.+)"
            change_match = re.match(change_pattern, change)
            if change_match:
                change_type = change_match.group(1).capitalize()
                message = change_match.group(2).strip()
                changelog_entries.append({
                    "author": author,
                    "type": change_type,
                    "message": message
                })
            else:
                print(f"Warning: Unable to parse change line: {change}")
    return changelog_entries

def get_last_id(changelog_data):
    if not changelog_data or "Entries" not in changelog_data or not changelog_data["Entries"]:
        return 0
    return max(entry["id"] for entry in changelog_data["Entries"])

def update_changelog():
    if not pr.body:
        print("PR body is empty.")
        return

    print("Original PR Body:", repr(pr.body))
    cleaned_body = remove_comments(pr.body)
    print("Cleaned PR Body:", repr(cleaned_body))

    if ":cl:" in cleaned_body:
        print("Found ':cl:' in PR body after removing comments.")
        merge_time = pr.merged_at
        entries = parse_changelog(cleaned_body)

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

        last_id = 0
        for entry in entries:
            #shift PR number up two digits
            #add current ID to it
            # e.g., PR number 123, last_id 5 -> calculatedID = (123 * 100) + 5 = 12305
            calculatedID = (int(pr_number) * 100) + last_id
            changelog_entry = {
                "author": entry["author"],
                "changes": [{
                    "message": entry["message"],
                    "type": entry["type"]
                }],
                "id": calculatedID,
                "time": merge_time.isoformat(timespec='microseconds'),
                "url": f"https://github.com/{repo_name}/pull/{pr_number}"
            }
            changelog_data["Entries"].append(changelog_entry)
            last_id += 1

        os.makedirs(os.path.dirname(changelog_path), exist_ok=True)

        with open(changelog_path, "w", encoding='utf-8') as file:
            yaml.dump(changelog_data, file, allow_unicode=True)
            file.write('\n')
        print(f"Changelog updated and written to {changelog_path}")
    else:
        print("No ':cl:' tag found in PR body after removing comments.")
        return

if __name__ == "__main__":
    update_changelog()
