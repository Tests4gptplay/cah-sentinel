#!/usr/bin/env python3
"""Opt-in, offline import of an owner-supplied CAH Sentinel GIF candidates archive.

Usage:
    python tools/gif/import_from_archive.py --zip "path/to/CAH-Sentinel-GIF-Candidates-v0.4.zip" --dry-run
    python tools/gif/import_from_archive.py --zip "path/to/CAH-Sentinel-GIF-Candidates-v0.4.zip"

Never runs git, modifies CAH, fetches remote resources or publishes artwork.
The imported ZipZipPipe-derived GIFs retain CC BY-NC-SA 4.0 independently of AGPL code.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import tempfile
import zipfile


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def verify_and_import(repo: Path, archive: Path, *, dry_run: bool = False,
                      overwrite: bool = False) -> list[str]:
    repo = repo.resolve()
    archive = archive.resolve()
    if not (repo / "LICENSE").is_file() or not (repo / "src" / "CAHSentinel").is_dir():
        raise ValueError("Destination does not look like CAH Sentinel's source repository.")
    asset_root = repo / "assets" / "animation-candidates"
    manifest = json.loads((asset_root / "manifest.v0.4.json").read_text(encoding="utf-8"))
    if not archive.is_file():
        raise FileNotFoundError(archive)
    if sha256(archive.read_bytes()) != manifest["owner_archive"]["sha256"] or archive.stat().st_size != manifest["owner_archive"]["bytes"]:
        raise ValueError("ZIP SHA-256 mismatch: refusing unverified content.")
    pending: list[tuple[Path, bytes]] = []
    with zipfile.ZipFile(archive, "r") as z:
        bad = z.testzip()
        if bad is not None:
            raise ValueError("Corrupt archive member: " + bad)
        for item in manifest["assets"]:
            member = item["path"]
            parts = Path(member).parts
            if not parts or parts[0] != "states" or any(p in ("", "..", ".") for p in parts) or not member.endswith(".gif"):
                raise ValueError("Invalid GIF resource path: " + member)
            payload = z.read(member)
            if sha256(payload) != item["sha256"] or len(payload) != item["size_bytes"]:
                raise ValueError("GIF digest or byte length mismatch: " + member)
            target = (asset_root / member).resolve()
            if not target.is_relative_to(asset_root.resolve()):
                raise ValueError("GIF destination escapes the asset folder.")
            if target.is_file():
                if sha256(target.read_bytes()) == item["sha256"]:
                    continue
                if not overwrite:
                    raise FileExistsError("File exists with different content: " + str(target))
            pending.append((target, payload))
    for target, payload in pending:
        if dry_run:
            print("WOULD IMPORT", target.relative_to(repo), len(payload), "bytes")
        else:
            target.parent.mkdir(parents=True, exist_ok=True)
            fd, temp_path = tempfile.mkstemp(prefix=".sentinel-", suffix=".tmp", dir=target.parent)
            try:
                with os.fdopen(fd, "wb") as file:
                    file.write(payload)
                os.replace(temp_path, target)
            finally:
                if Path(temp_path).exists():
                    Path(temp_path).unlink()
            print("IMPORTED", target.relative_to(repo), len(payload), "bytes")
    if not pending:
        print("All candidate GIFs are already present with matching hashes.")
    elif not dry_run:
        print("Imported locally; Git add/commit/push is still required for GitHub.")
    return [str(path.relative_to(repo)).replace(os.sep, "/") for path, _ in pending]


def main() -> None:
    parser = argparse.ArgumentParser(description="Offline import of SHA-verified GIF candidates")
    parser.add_argument("--zip", type=Path, required=True, help="Owner-supplied GIF ZIP file")
    parser.add_argument("--repo", type=Path, default=Path(__file__).resolve().parents[2],
                        help="Local CAH Sentinel checkout")
    parser.add_argument("--dry-run", action="store_true", help="Verify ZIP but write nothing")
    parser.add_argument("--overwrite", action="store_true", help="Explicitly replace differing GIFs")
    args = parser.parse_args()
    verify_and_import(args.repo, args.zip, dry_run=args.dry_run, overwrite=args.overwrite)


if __name__ == "__main__":
    main()
