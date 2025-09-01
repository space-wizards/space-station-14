import os
import json
from github import Github

print("=== Backfill Changelog Finder Script Started ===")

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
repo = g.get_repo(repo_name)

def extract_pr_number_from_commit_message(commit_message):
    """Extract PR number from merge commit message"""
    import re
    # Pattern for "Merge pull request #123 from..."
    pr_pattern = r"Merge pull request #(\d+)"
    match = re.search(pr_pattern, commit_message)
    if match:
        return int(match.group(1))
    return None

def get_existing_changelog_entries():
    """Load existing changelog entries and return a set of existing PR numbers"""
    import yaml
    import re
    
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

def find_missing_changelog_prs():
    """Find PRs that need changelog entries and return their numbers"""
    # Get existing changelog entries to avoid duplicates
    existing_pr_numbers = get_existing_changelog_entries()
    
    print(f"\n=== Checking last {commits_count} commits ===")
    
    # Get recent commits from the main branch
    commits = list(repo.get_commits(sha='Starlight'))[:commits_count]
    
    missing_prs = []
    processed_count = 0
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
            # Get the PR to check if it's merged and has body
            pr = repo.get_pull(pr_number)
            
            if not pr.merged:
                print(f"  PR #{pr_number} is not merged, skipping")
                continue
                
            print(f"  PR #{pr_number} title: {pr.title}")
            
            if not pr.body:
                print(f"  PR #{pr_number} has no body, skipping")
                continue
            
            # Check if PR body contains :cl: tag
            if ":cl:" not in pr.body:
                print(f"  No ':cl:' tag found in PR #{pr_number} body, skipping")
                continue
            
            print(f"  PR #{pr_number} needs changelog entry")
            missing_prs.append(pr_number)
            
        except Exception as e:
            print(f"  Error processing PR #{pr_number}: {str(e)}")
            continue
    
    print(f"\n=== Summary ===")
    print(f"Processed {processed_count} commits")
    print(f"Found {len(missing_prs)} PRs needing changelog entries")
    print(f"Skipped {skipped_count} PRs (already have changelog entries)")
    
    if missing_prs:
        print(f"Missing PRs: {missing_prs}")
        # Output as JSON for GitHub Actions
        print(f"::set-output name=missing_prs::{json.dumps(missing_prs)}")
        return missing_prs
    else:
        print("No missing changelog entries found")
        print("::set-output name=missing_prs::[]")
        return []

if __name__ == "__main__":
    find_missing_changelog_prs()
