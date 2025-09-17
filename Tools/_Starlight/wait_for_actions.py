from github import Github
from github import Auth
import os
import time

# using an access token
GITHUB_TOKEN = os.getenv("GITHUB_TOKEN")
REPO_NAME = os.getenv("GITHUB_REPOSITORY")
RUN_NUMBER = int(os.getenv("WORKFLOW_RUN_NUMBER"))
WORKFLOW_NAME = os.getenv("WORKFLOW_NAME")

g = Github(GITHUB_TOKEN)
repo = g.get_repo(REPO_NAME)
print(f"Authenticated to repo {repo.full_name}", flush=True)

def checkAllStatuses(statuses):
    for status in statuses:
        if not checkIfSafeToProceed(status):
            return False
    return True

def checkIfSafeToProceed(status):
    #check if there are any other workflow runs for this workflow that are queued or in progress with an ID less than us
    workflow_runs = repo.get_workflow_runs(status=status, exclude_pull_requests=False)
    #print all runs
    for wr in workflow_runs:
        print(f"Found workflow run {wr.name} with ID {wr.run_number} and status {wr.status}", flush=True)
    que_workflow_runs = [wr for wr in workflow_runs if wr.name == WORKFLOW_NAME]
    any_before_us = any(wr.run_number < RUN_NUMBER for wr in que_workflow_runs)
    return not any_before_us

#wait because the github API seems to be inconsistent and updates slowly?
time.sleep(20)

#loop and get all their IDs, and check if ANY are less than us
while True:
    if checkAllStatuses(["queued", "in_progress", "requested", "waiting", "pending"]):
        print("No workflow runs queued or running before us, proceeding...", flush=True)
        exit(0)

    print("There are still workflow runs queued or running before us, waiting 10 seconds...", flush=True)
    time.sleep(10)
