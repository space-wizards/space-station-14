#!/usr/bin/env python3

import argparse
import requests
import itertools
import signal
import sdnotify
import os
from io import StringIO
from abc import ABC, abstractmethod
from enum import Enum
from dataclasses import dataclass
from typing import Iterable, TypeVar, Callable, Any, Optional
from prometheus_client import start_http_server, CollectorRegistry
from prometheus_client.core import Metric, GaugeMetricFamily
import time
# from prometheus_client.registry import Collector

GITHUB_API_URL = "https://api.github.com/graphql"
GITHUB_TOKEN = os.environ["SS14_REPO_STATS_GITHUB_TOKEN"]
USER_AGENT = "github_repo_stats.py/1.0.0"

# We have a GraphQL client at home

class AbstractQuery(ABC):
    @abstractmethod
    def get_selector(self, alias: str):
        pass

class IssueState(Enum):
    CLOSED = 1,
    OPEN = 2,

class IssueQuery(AbstractQuery):
    def __init__(self, states: Optional[list[IssueState]] = None, labels: Optional[list[str]] = None) -> None:
        super().__init__()

        self.states = states
        self.labels = labels

    def get_selector(self, alias: str):
        query_args = []
        if self.states and len(self.states):
            query_args.append(f"states:{enum_join(self.states)}")
        if self.labels and len(self.labels):
            query_args.append(f"labels:{labels_join(self.labels)}")

        args = f"({','.join(query_args)})" if len(query_args) else ""

        return f"{alias}: issues{args}"

class PullRequestState(Enum):
    CLOSED = 1,
    MERGED = 2
    OPEN = 3,

class PullRequestQuery(AbstractQuery):
    def __init__(self, states: Optional[list[PullRequestState]] = None, labels: Optional[list[str]] = None) -> None:
        super().__init__()

        self.states = states
        self.labels = labels

    def get_selector(self, alias: str):
        query_args = []
        if self.states and len(self.states):
            query_args.append(f"states:{enum_join(self.states)}")

        if self.labels and len(self.labels):
            query_args.append(f"labels:{labels_join(self.labels)}")

        args = f"({','.join(query_args)})" if len(query_args) else ""

        return f"{alias}: pullRequests{args}"

@dataclass
class Repo:
    owner: str
    name: str

    queries: dict[str, AbstractQuery]

LABEL_UNTRIAGED = "S: Untriaged"
LABEL_NEEDS_REVIEW = "S: Needs Review"
LABEL_AWAITING_CHANGES = "S: Awaiting Changes"
LABEL_APPROVED = "S: Approved"
LABEL_P0 = "P0: Critical"
LABEL_P1 = "P1: High"
LABEL_CONFLICT = "S: Merge Conflict"

REPO_CONFIG = [
    Repo("space-wizards", "space-station-14", queries={
        # Issue queries
        "issue_total_count": IssueQuery(),
        "issue_open_count": IssueQuery(states=[IssueState.OPEN]),
        "issue_closed_count": IssueQuery(states=[IssueState.CLOSED]),
        "issue_untriaged_count": IssueQuery(states=[IssueState.OPEN], labels=[LABEL_UNTRIAGED]),

        # PR queries
        "pr_total_count": PullRequestQuery(),
        "pr_open_count": PullRequestQuery(states=[PullRequestState.OPEN]),
        "pr_closed_count": PullRequestQuery(states=[PullRequestState.CLOSED]),
        "pr_merged_count": PullRequestQuery(states=[PullRequestState.MERGED]),
        "pr_untriaged_count": PullRequestQuery(states=[PullRequestState.OPEN], labels=[LABEL_UNTRIAGED]),
        "pr_needs_review_count": PullRequestQuery(states=[PullRequestState.OPEN], labels=[LABEL_NEEDS_REVIEW]),
        "pr_awaiting_changes_count": PullRequestQuery(states=[PullRequestState.OPEN], labels=[LABEL_AWAITING_CHANGES]),
        "pr_approved_count": PullRequestQuery(states=[PullRequestState.OPEN], labels=[LABEL_APPROVED]),
        "pr_p0_count": PullRequestQuery(states=[PullRequestState.OPEN], labels=[LABEL_P0]),
        "pr_p1_count": PullRequestQuery(states=[PullRequestState.OPEN], labels=[LABEL_P1]),
        "pr_conflict_count": PullRequestQuery(states=[PullRequestState.OPEN], labels=[LABEL_CONFLICT]),
    }),
    Repo("space-wizards", "RobustToolbox", queries={
        # Issue queries
        "issue_total_count": IssueQuery(),
        "issue_open_count": IssueQuery(states=[IssueState.OPEN]),
        "issue_closed_count": IssueQuery(states=[IssueState.CLOSED]),
        "issue_untriaged_count": IssueQuery(states=[IssueState.OPEN], labels=[LABEL_UNTRIAGED]),

        # PR queries
        "pr_total_count": PullRequestQuery(),
        "pr_open_count": PullRequestQuery(states=[PullRequestState.OPEN]),
        "pr_closed_count": PullRequestQuery(states=[PullRequestState.CLOSED]),
        "pr_merged_count": PullRequestQuery(states=[PullRequestState.MERGED]),
        "pr_untriaged_count": PullRequestQuery(states=[PullRequestState.OPEN], labels=[LABEL_UNTRIAGED]),
        "pr_needs_review_count": PullRequestQuery(states=[PullRequestState.OPEN], labels=[LABEL_NEEDS_REVIEW]),
        "pr_awaiting_changes_count": PullRequestQuery(states=[PullRequestState.OPEN], labels=[LABEL_AWAITING_CHANGES]),
    }),
    Repo("space-wizards", "docs", queries={
        # Issue queries
        "issue_total_count": IssueQuery(),
        "issue_open_count": IssueQuery(states=[IssueState.OPEN]),
        "issue_closed_count": IssueQuery(states=[IssueState.CLOSED]),

        # PR queries
        "pr_total_count": PullRequestQuery(),
        "pr_open_count": PullRequestQuery(states=[PullRequestState.OPEN]),
        "pr_closed_count": PullRequestQuery(states=[PullRequestState.CLOSED]),
        "pr_merged_count": PullRequestQuery(states=[PullRequestState.MERGED]),
    }),
]

E = TypeVar("E", bound=Enum)
def enum_join(iterable: Iterable[E]) -> str:
    return f"[{','.join((e.name for e in iterable))}]"

def labels_join(iterable: Iterable[str]) -> str:
    contents = ','.join(map(graphql_string, iterable))
    return f"[{contents}]"

T = TypeVar("T")
def first_or_default(iterable: Iterable[T], func: Callable[[T], bool]) -> Optional[T]:
    for val in iterable:
        if func(val):
            return val

    return None

REQUESTS_SESSION = requests.Session()
REQUESTS_SESSION.headers["User-Agent"] = USER_AGENT

def graphql_string(val: str) -> str:
    # This is probably good enough.
    # Note that this script doesn't accept (potentially malicious) user input.
    # If it did, I wouldn't be doing it like this.
    val_replaced = val.replace('"', '\\"')
    return f"\"{val_replaced}\""

def generate_graphql_query_for_repo(repo: Repo) -> str:
    strio = StringIO()

    strio.write("query {\n")
    strio.write(f"\trepository(owner: {graphql_string(repo.owner)}, name: {graphql_string(repo.name)})" + " {\n")

    for key, query in repo.queries.items():
        strio.write(f"\t\t{query.get_selector(key)}" + " {\n")
        strio.write("\t\t\ttotalCount\n")
        strio.write("\t\t}\n")

    strio.write("\t}\n")
    strio.write("}\n")

    return strio.getvalue()

def repo_key(repo: Repo) -> str:
    return f"{repo.owner}/{repo.name}"

def do_graphql_query(query: str) -> Any:
    with REQUESTS_SESSION.post(GITHUB_API_URL, headers={"Authorization": f"Bearer {GITHUB_TOKEN}"}, json={"query": query}) as resp:
        resp.raise_for_status()
        return resp.json()

CACHED_QUERIES = {repo_key(repo): generate_graphql_query_for_repo(repo) for repo in REPO_CONFIG}

# RHEL 9 ships with an old version of prometheus_client that doesn't have the "Collector" type...
class StatsCollector: #(Collector):
    def collect(self) -> Iterable[Metric]:
        query_time_metric = GaugeMetricFamily("repo_stats_query_time", "Time the GitHub query API call took", labels=["repo"])
        metrics: dict[str, GaugeMetricFamily] = {}
        # Make all the metric families.
        for repo in REPO_CONFIG:
            r_key = repo_key(repo)
            for key in repo.queries.keys():
                if key not in metrics:
                    metrics[key] = GaugeMetricFamily(f"repo_stats_{key}", key, labels=["repo"])

            # Do the requests and fill out the stats.
            query = CACHED_QUERIES[r_key]
            start_time = time.monotonic_ns()
            query_data = do_graphql_query(query)
            end_time = time.monotonic_ns()
            repo_data = query_data["data"]["repository"]

            for key in repo.queries.keys():
                value = repo_data[key]["totalCount"]
                metrics[key].add_metric([r_key], float(value))

            query_time_metric.add_metric([r_key], (end_time - start_time) / 1_000_000_000)

        yield from metrics.values()
        yield query_time_metric

def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("port", type=int)
    parser.add_argument("--host", default="0.0.0.0")

    args = parser.parse_args()

    port = args.port
    host = args.host

    registry = CollectorRegistry(auto_describe=True)
    registry.register(StatsCollector())

    start_http_server(port, host, registry)

    sdnotify.SystemdNotifier().notify("READY=1")

    signal.pause()

main()
