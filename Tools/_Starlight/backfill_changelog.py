import os
import yaml
import re
from datetime import datetime
from typing import Set, List, Dict, Any, Optional
from github import Github
from github.Repository import Repository
from github.PullRequest import PullRequest

print("=== Backfill Changelog Script Started ===")

print("Environment Variables:")
changelog_path = os.getenv("CHANGELOG_FILE_PATH")
repo_name = os.getenv("GITHUB_REPOSITORY")
github_token = os.getenv("GITHUB_TOKEN")
commits_count = int(os.getenv("COMMITS_COUNT", "10"))

print(f"CHANGELOG_FILE_PATH: {changelog_path}")
print(f"GITHUB_REPOSITORY: {repo_name}")
print(f"GITHUB_TOKEN is set: {bool(github_token)}")
print(f"COMMITS_COUNT: {commits_count}")

# Validate required environment variables
if not changelog_path:
    raise ValueError("CHANGELOG_FILE_PATH environment variable is required")
if not repo_name:
    raise ValueError("GITHUB_REPOSITORY environment variable is required")
if not github_token:
    raise ValueError("GITHUB_TOKEN environment variable is required")

g = Github(github_token)
repo: Repository = g.get_repo(repo_name)

def remove_comments(pr_body):
    """
    Removes all content inside HTML comments <!-- --> from the PR body.
    """
    if not pr_body:
        return ""
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

def extract_pr_number_from_commit_message(commit_message):
    """Extract PR number from merge commit message"""
    # Pattern for "Merge pull request #123 from..."
    pr_pattern = r"Merge pull request #(\d+)"
    match = re.search(pr_pattern, commit_message)
    if match:
        return int(match.group(1))
    return None

def get_existing_changelog_entries():
    """Load existing changelog entries and return a set of existing PR numbers"""
    existing_entries = set()
    
    # We know changelog_path is not None due to validation above
    assert changelog_path is not None
    
    if os.path.exists(changelog_path):
        print(f"Loading existing changelog from {changelog_path}")
        with open(changelog_path, "r", encoding='utf-8') as file:
            changelog_data = yaml.safe_load(file)
            if changelog_data and "Entries" in changelog_data:
                for entry in changelog_data["Entries"]:
                    # Extract PR number from URL
                    if "url" in entry:
                        url_match = re.search(r"/pull/(\d+)$", entry["url"])
                        if url_match:
                            existing_entries.add(int(url_match.group(1)))
                    # Also check if ID encodes PR number (if using the PR*100+offset scheme)
                    if "id" in entry and entry["id"] > 100:
                        potential_pr = entry["id"] // 100
                        existing_entries.add(potential_pr)
    else:
        print(f"Changelog file does not exist at {changelog_path}")
    
    print(f"Found {len(existing_entries)} existing changelog entries for PRs: {sorted(existing_entries)}")
    return existing_entries

def add_changelog_entry(changelog_data, pr, pr_number, entries):
    """Add changelog entries for a specific PR"""
    if not entries:
        return False
    
    last_id_in_pr = 0
    entries_added = 0
    
    for entry in entries:
        # Calculate ID using PR number * 100 + offset
        calculated_id = (pr_number * 100) + last_id_in_pr
        
        changelog_entry = {
            "author": entry["author"],
            "changes": [{
                "message": entry["message"],
                "type": entry["type"]
            }],
            "id": calculated_id,
            "time": pr.merged_at.isoformat(timespec='microseconds') if pr.merged_at else datetime.now().isoformat(timespec='microseconds'),
            "url": f"https://github.com/{repo_name}/pull/{pr_number}"
        }
        
        changelog_data["Entries"].append(changelog_entry)
        last_id_in_pr += 1
        entries_added += 1
        print(f"  Added changelog entry for PR #{pr_number} (ID: {calculated_id}): {entry['type']} - {entry['message']}")
    
    return entries_added > 0

def backfill_changelog():
    # Get existing changelog entries to avoid duplicates
    existing_pr_numbers = get_existing_changelog_entries()
    
    # We know changelog_path is not None due to validation above
    assert changelog_path is not None
    
    # Load existing changelog or create new one
    if os.path.exists(changelog_path):
        with open(changelog_path, "r", encoding='utf-8') as file:
            changelog_data = yaml.safe_load(file) or {"Entries": []}
    else:
        changelog_data = {"Entries": []}
    
    print(f"\n=== Checking last {commits_count} commits ===")
    
    # Get recent commits from the main branch
    commits = repo.get_commits(sha='Starlight', per_page=commits_count)
    
    processed_count = 0
    added_count = 0
    skipped_count = 0
    
    for commit in commits:
        processed_count += 1
        commit_message = commit.commit.message
        print(f"\n--- Commit {processed_count}/{commits_count} ---")
        print(f"Commit SHA: {commit.sha[:8]}")
        print(f"Commit message: {commit_message.split(chr(10))[0]}")  # First line only
        
        # Extract PR number from commit message
        pr_number = extract_pr_number_from_commit_message(commit_message)
        
        if not pr_number:
            print("  No PR number found in commit message, skipping")
            continue
            
        print(f"  Found PR number: {pr_number}")
        
        # Check if we already have changelog entry for this PR
        if pr_number in existing_pr_numbers:
            print(f"  Changelog entry for PR #{pr_number} already exists, skipping")
            skipped_count += 1
            continue
        
        try:
            # Get the PR
            pr = repo.get_pull(pr_number)
            
            if not pr.merged:
                print(f"  PR #{pr_number} is not merged, skipping")
                continue
                
            print(f"  PR #{pr_number} title: {pr.title}")
            
            if not pr.body:
                print(f"  PR #{pr_number} has no body, skipping")
                continue
            
            # Parse PR body for changelog entries
            cleaned_body = remove_comments(pr.body)
            
            if ":cl:" not in cleaned_body:
                print(f"  No ':cl:' tag found in PR #{pr_number} body, skipping")
                continue
                
            entries = parse_changelog(cleaned_body)
            
            if not entries:
                print(f"  No valid changelog entries found in PR #{pr_number}, skipping")
                continue
            
            # Add entries to changelog
            if add_changelog_entry(changelog_data, pr, pr_number, entries):
                added_count += 1
                existing_pr_numbers.add(pr_number)  # Track this PR as processed
                print(f"  Successfully added {len(entries)} changelog entries for PR #{pr_number}")
            
        except Exception as e:
            print(f"  Error processing PR #{pr_number}: {str(e)}")
            continue
    
    # Sort entries by ID to maintain order
    changelog_data["Entries"].sort(key=lambda x: x.get("id", 0))
    
    # Write updated changelog
    if added_count > 0:
        # We know changelog_path is not None due to validation above
        assert changelog_path is not None
        os.makedirs(os.path.dirname(changelog_path), exist_ok=True)
        with open(changelog_path, "w", encoding='utf-8') as file:
            yaml.dump(changelog_data, file, allow_unicode=True)
            file.write('\n')
        print(f"\n=== Summary ===")
        print(f"Processed {processed_count} commits")
        print(f"Added changelog entries for {added_count} PRs")
        print(f"Skipped {skipped_count} PRs (already have changelog entries)")
        print(f"Changelog updated and written to {changelog_path}")
    else:
        print(f"\n=== Summary ===")
        print(f"Processed {processed_count} commits")
        print(f"No new changelog entries added - all recent PRs already have changelog entries")

if __name__ == "__main__":
    backfill_changelog()
