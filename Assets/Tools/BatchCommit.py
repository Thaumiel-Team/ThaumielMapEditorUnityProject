"""
Usage examples

Commit everything in the repo, 500 files per commit, pushing after each commit
    python batch_commit.py --repo /path/to/repo

Custom batch size, dry run first to see the plan, then commit without pushing
    python batch_commit.py --repo /path/to/repo --batch-size 200 --dry-run
    python batch_commit.py --repo /path/to/repo --batch-size 200 --push no

Custom commit message prefix
    python batch_commit.py --repo /path/to/repo -m "Import legacy assets"
"""

import argparse
import subprocess
import sys
from pathlib import Path

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        _stream.reconfigure(encoding="utf-8", errors="replace")


def run(cmd, cwd, check=True):
    result = subprocess.run(
        cmd,
        cwd=cwd,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
    )
    if check and result.returncode != 0:
        print(f"Command failed: {' '.join(cmd)}", file=sys.stderr)
        print(result.stdout, file=sys.stderr)
        print(result.stderr, file=sys.stderr)
        sys.exit(1)
    return result


def get_changed_files(repo_path):
    result = run(["git", "status", "--porcelain=v1", "-z"], cwd=repo_path)
    raw = result.stdout
    if not raw:
        return []

    entries = raw.split("\x00")
    paths = []
    i = 0
    while i < len(entries):
        entry = entries[i]
        if not entry:
            i += 1
            continue
        status = entry[:2]
        path = entry[3:]
        if status.startswith("R"):
            i += 1
        paths.append(path)
        i += 1
    return paths


def chunked(items, size):
    for i in range(0, len(items), size):
        yield items[i:i + size]


def main():
    parser = argparse.ArgumentParser(description="Commit files to a git repo in batches.")
    parser.add_argument("--repo", required=True, help="Path to the local git repository")
    parser.add_argument("--batch-size", type=int, default=500, help="Files per commit (default: 500)")
    parser.add_argument("-m", "--message", default="Batch commit", help="Base commit message")
    parser.add_argument(
        "--push", choices=["yes", "no"], default="yes",
        help="Push immediately after each batch is committed (default: yes)"
    )
    parser.add_argument("--branch", default=None, help="Branch to push to (default: current branch)")
    parser.add_argument("--remote", default="origin", help="Remote name to push to (default: origin)")
    parser.add_argument("--dry-run", action="store_true", help="Show the batch plan without committing")
    args = parser.parse_args()

    repo_path = Path(args.repo).resolve()
    if not (repo_path / ".git").exists():
        print(f"Error: {repo_path} does not look like a git repository (no .git dir).", file=sys.stderr)
        sys.exit(1)

    files = get_changed_files(repo_path)
    if not files:
        print("No changed or untracked files found. Nothing to do.")
        return

    batches = list(chunked(files, args.batch_size))
    total = len(batches)
    print(f"Found {len(files)} changed file(s) -> {total} batch(es) of up to {args.batch_size} files each.\n")

    if args.dry_run:
        for i, batch in enumerate(batches, 1):
            print(f"Batch {i}/{total}: {len(batch)} files")
            for f in batch[:5]:
                print(f"    {f}")
            if len(batch) > 5:
                print(f"    ... and {len(batch) - 5} more")
        print("\nDry run only — no changes were made.")
        return

    for i, batch in enumerate(batches, 1):
        print(f"Adding batch {i}/{total} ({len(batch)} files)...")
        run(["git", "add", "--"] + batch, cwd=repo_path)

        commit_msg = f"{args.message} (batch {i}/{total})"
        commit_result = run(["git", "commit", "-m", commit_msg], cwd=repo_path, check=False)
        if commit_result.returncode != 0:
            print(f"  Skipped commit for batch {i} (nothing staged?):")
            print(f"  {commit_result.stdout.strip()}")
            continue

        print(f"  Committed: {commit_msg}")

        if args.push == "yes":
            push_batch(repo_path, args.remote, args.branch)

    print("\nDone.")


def push_batch(repo_path, remote, branch):
    cmd = ["git", "push", remote]
    if branch:
        cmd.append(branch)
    print(f"  Pushing to {remote}{' ' + branch if branch else ''}...")
    run(cmd, cwd=repo_path)


if __name__ == "__main__":
    main()