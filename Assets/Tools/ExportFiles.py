"""
Options:
    --source-root PATH     Root folder that exported paths are kept relative to (default: the Assets folder containing this script)
    --dest-root PATH       Folder to export into (default: <project>/ExportedCode)
    --overwrite            Replace files that already exist at the destination
    --dry-run              Preview everything without writing any files
    --yes                  Skip the confirmation prompt for directories

At prompt:
    <path to directory>    export that directory and everything inside it
    <path to file>         export that single file (and its .meta, if present)
    dest                   show the current export folder
    dest <path>            change the export folder for the rest of the session (relative paths use the current working directory)
    source                 show the source root
    help                   show the commands
    quit / exit / q        stop
"""

import argparse
import shutil
import sys
from pathlib import Path


META_SUFFIX = ".meta"

SKIP_DIR_NAMES = {"library", "temp", "obj", "logs", ".git", ".vs"}


def is_meta(path: Path) -> bool:
    return path.suffix.lower() == META_SUFFIX


def is_within(path: Path, base: Path) -> bool:
    try:
        path.relative_to(base)
        return True
    except ValueError:
        return False


def human_size(num_bytes: float) -> str:
    for unit in ("B", "KB", "MB", "GB", "TB"):
        if num_bytes < 1024 or unit == "TB":
            return f"{int(num_bytes)} {unit}" if unit == "B" else f"{num_bytes:.1f} {unit}"
        num_bytes /= 1024
    return f"{num_bytes:.1f} TB"


def display(path: Path, bases) -> str:
    for base in bases:
        try:
            return str(path.relative_to(base))
        except ValueError:
            continue
    return str(path)


def collect_directory_files(directory: Path, dest_root: Path):
    files = []
    dest_inside = is_within(dest_root, directory)

    for path in directory.rglob("*"):
        if not path.is_file():
            continue

        relative = path.relative_to(directory)
        if any(part.lower() in SKIP_DIR_NAMES for part in relative.parts[:-1]):
            continue

        if dest_inside and is_within(path, dest_root):
            continue

        files.append(path)

    return sorted(files)


def destination_for_directory(directory: Path, source_root: Path,
                              dest_root: Path) -> Path:
    try:
        return dest_root / directory.relative_to(source_root)
    except ValueError:
        return dest_root / directory.name


def build_directory_items(directory: Path, dest_dir: Path, dest_root: Path):
    return [
        (path, dest_dir / path.relative_to(directory))
        for path in collect_directory_files(directory, dest_root)
    ]


def report_directory_plan(directory: Path, dest_dir: Path, items, source_root: Path,
                          overwrite: bool):
    print(f"\nDirectory: {directory}")
    print(f"      ->   {dest_dir}")
    print(f"\nFiles to export ({len(items)}):")

    total_bytes = 0
    existing = 0
    for source, dest in items:
        try:
            total_bytes += source.stat().st_size
        except OSError:
            pass

        marker = ""
        if dest.exists():
            existing += 1
            marker = "  [replace]" if overwrite else "  [already exists, skipped]"

        print(f"  {display(source, [source_root])}{marker}")

    print(f"\n  {len(items)} file(s), {human_size(total_bytes)}")
    if existing:
        if overwrite:
            print(f"  {existing} file(s) already exist and will be replaced.")
        else:
            print(f"  {existing} file(s) already exist and will be skipped "
                  f"(use --overwrite to replace them).")


def confirm(prompt: str) -> bool:
    try:
        answer = input(prompt).strip().lower()
    except (EOFError, KeyboardInterrupt):
        print()
        return False
    return answer in ("y", "yes")


def copy_file(source: Path, dest: Path, overwrite: bool, dry_run: bool) -> bool:
    if source.resolve() == dest.resolve():
        print(f"    SKIPPED (source and destination are the same): {source}")
        return False

    if dest.exists():
        if not overwrite:
            print(f"    SKIPPED (already exists): {dest}")
            return False
        if dest.is_dir():
            print(f"    SKIPPED (a folder exists at the destination): {dest}")
            return False
        if not dry_run:
            dest.unlink()

    if dry_run:
        print(f"    [dryrun] {source} -> {dest}")
        return True

    dest.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, dest)
    return True


def export_directory(directory: Path, source_root: Path, dest_root: Path,
                     overwrite: bool, dry_run: bool, assume_yes: bool) -> bool:
    if not is_within(directory, source_root):
        print("  NOTE: this folder is outside the source root; it will be "
              "exported under its own name.")

    dest_dir = destination_for_directory(directory, source_root, dest_root)
    if directory.resolve() == dest_dir.resolve():
        print("\n  The source and export folders are the same; nothing to export.")
        return False

    items = build_directory_items(directory, dest_dir, dest_root)
    if not items:
        print(f"\nNo files found in {directory}.")
        return False

    report_directory_plan(directory, dest_dir, items, source_root, overwrite)

    to_copy = [(source, dest) for source, dest in items
               if overwrite or not dest.exists()]
    if not to_copy:
        print("\n  Nothing to export (everything already exists).")
        return False

    if not dry_run and not assume_yes:
        verb = "Replace" if overwrite else "Export"
        if not confirm(f"\n  {verb} {len(to_copy)} file(s)? [y/N] "):
            print("  Cancelled.")
            return False

    print()
    copied = 0
    skipped = 0
    for source, dest in items:
        if copy_file(source, dest, overwrite, dry_run):
            copied += 1
        else:
            skipped += 1

    if dry_run:
        print(f"\n  Dry run: {copied} file(s) would be exported, {skipped} skipped.")
    else:
        print(f"\n  Done: {copied} file(s) exported, {skipped} skipped.")
    return copied > 0 and not dry_run


def export_single_file(source: Path, source_root: Path, dest_root: Path,
                       overwrite: bool, dry_run: bool) -> bool:
    print(f"\nFile: {source}")

    try:
        dest = dest_root / source.relative_to(source_root)
    except ValueError:
        dest = dest_root / source.name
        print("  (outside the source root; exporting it by name)")

    print(f"    -> {dest}")

    pairs = [(source, dest)]
    if not is_meta(source):
        meta = source.with_name(source.name + META_SUFFIX)
        if meta.is_file():
            pairs.append((meta, dest.with_name(dest.name + META_SUFFIX)))

    copied = 0
    skipped = 0
    for item_source, item_dest in pairs:
        if copy_file(item_source, item_dest, overwrite, dry_run):
            copied += 1
        else:
            skipped += 1

    if dry_run:
        print(f"\n  Dry run: {copied} file(s) would be exported, {skipped} skipped.")
    else:
        print(f"\n  Done: {copied} file(s) exported, {skipped} skipped.")
    return copied > 0 and not dry_run


def print_help():
    print("\nCommands:")
    print("  <file or directory>  export that path")
    print("  dest                 show the current export folder")
    print("  dest <path>          change the export folder")
    print("  source               show the source root")
    print("  help                 show this help")
    print("  quit / exit / q      stop")
    print("\nRelative paths are tried against the source root first, then the "
          "current working directory.")


def strip_quotes(text: str) -> str:
    return text.strip().strip('"').strip("'")


def resolve_target(text: str, source_root: Path) -> Path:
    raw = Path(strip_quotes(text))
    if raw.is_absolute():
        return raw.resolve()

    source_candidate = source_root / raw
    if source_candidate.exists():
        return source_candidate.resolve()

    cwd_candidate = Path.cwd() / raw
    if cwd_candidate.exists():
        return cwd_candidate.resolve()

    return source_candidate.resolve()


def resolve_destination(text: str) -> Path:
    raw = Path(strip_quotes(text))
    if raw.is_absolute():
        return raw.resolve()
    return (Path.cwd() / raw).resolve()


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
        sys.stderr.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass

    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    default_source = Path(__file__).resolve().parent.parent

    parser.add_argument("--source-root", type=Path, default=default_source,
                        help="Root folder that exported paths are kept relative to "
                             "(default: the Assets folder containing this script)")
    parser.add_argument("--dest-root", type=Path, default=None,
                        help="Folder to export into (default: <project>/ExportedCode)")
    parser.add_argument("--overwrite", action="store_true",
                        help="Replace files that already exist at the destination")
    parser.add_argument("--dry-run", action="store_true",
                        help="Preview everything without writing any files")
    parser.add_argument("--yes", action="store_true",
                        help="Skip the confirmation prompt for directories")
    args = parser.parse_args()

    source_root = args.source_root.resolve()
    if not source_root.is_dir():
        print(f"Source root is not a folder: {source_root}")
        return 1

    dest_root = (args.dest_root or source_root.parent / "ExportedCode").resolve()

    print(f"Source root: {source_root}")
    print(f"Export to:   {dest_root}")
    if is_within(dest_root, source_root):
        print("NOTE: the export folder is inside the source root; exported copies "
              "will be ignored when selecting that part of the project again.")
    print("Files keep their path relative to the source root. Originals are not changed.")
    print("Type a file or directory to export. Type 'help' for commands.\n")

    while True:
        try:
            line = input("export> ").strip()
        except (EOFError, KeyboardInterrupt):
            print("\nExiting.")
            break

        if not line:
            continue

        parts = line.split(maxsplit=1)
        command = parts[0].lower()
        argument = parts[1].strip() if len(parts) > 1 else ""

        if command in ("quit", "exit", "q"):
            print("Exiting.")
            break
        if command in ("help", "?"):
            print_help()
            continue
        if command == "source":
            print(f"  {source_root}")
            continue
        if command == "dest":
            if not argument:
                print(f"  {dest_root}")
            else:
                dest_root = resolve_destination(argument)
                print(f"  Export folder is now: {dest_root}")
            continue

        target = resolve_target(line, source_root)
        if not target.exists():
            print(f"  Not found: {target}")
            print("  Relative paths are tried against the source root and the "
                  "current working directory.")
            continue

        if target.is_dir():
            export_directory(target, source_root, dest_root, args.overwrite,
                             args.dry_run, args.yes)
        elif target.is_file():
            export_single_file(target, source_root, dest_root, args.overwrite,
                               args.dry_run)
        else:
            print(f"  Not a file or directory: {target}")
        print()

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
