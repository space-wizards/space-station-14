from github import Github
from github import Auth
import os
import time

# using an access token
github_token = os.getenv("GITHUB_TOKEN")
repo_name = os.getenv("GITHUB_REPOSITORY")
workflow_run_id = int(os.getenv("WORKFLOW_RUN_ID"))
workflow_name = os.getenv("WORKFLOW_NAME")

auth = Auth.Token(github_token)
g = Github(auth=auth)
repo = g.get_repo(repo_name)
print(f"Authenticated to repo {repo.full_name}")

#now find the workflow run list for the que workflow, and see if there is any before us
workflow_runs = repo.get_workflow_runs()

#loop and get all their IDs, and check if ANY are less than us
while True:
    workflow_runs = repo.get_workflow_runs()
    #get all the ones running the specific workflow
    que_workflow_runs = [wr for wr in workflow_runs if wr.name == workflow_name and wr.status in ["queued", "in_progress"]]
    #check them all for if any of them have an ID less than us
    any_before_us = any(wr.id < workflow_run_id for wr in que_workflow_runs)
    if not any_before_us:
        print("No workflow runs queued or running before us, proceeding...")
        break

    print("There are still workflow runs queued or running before us, waiting 10 seconds...", flush=True)
    time.sleep(10)
