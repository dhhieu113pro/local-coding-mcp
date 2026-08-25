#!/usr/bin/env python3
"""Validate LocalCodingMcp.Dnx and exercise it over MCP stdio."""

from __future__ import annotations

import json
import os
from pathlib import Path
import queue
import shutil
import subprocess
import sys
import tempfile
import threading
import time
import zipfile

PACKAGE_ID = "LocalCodingMcp.Dnx"
TOOL_COMMAND = "LocalCodingMcp"


def fail(message: str) -> None:
    raise RuntimeError(message)


def package_path(directory: Path, version: str) -> Path:
    expected = directory / f"{PACKAGE_ID}.{version}.nupkg"
    if not expected.is_file():
        fail(f"Missing package: {expected.name}")
    return expected


def validate_package(package: Path, version: str) -> None:
    if not package.with_suffix(".snupkg").is_file():
        fail("Symbol package is missing")

    with zipfile.ZipFile(package) as archive:
        names = set(archive.namelist())
        required = {
            ".mcp/server.json",
            "README.md",
            "LICENSE",
            "tools/net10.0/any/DotnetToolSettings.xml",
            "tools/net10.0/any/LocalCodingMcp.Dnx.dll",
            "tools/net10.0/any/LocalCodingMcp.Dnx.runtimeconfig.json",
        }
        missing = sorted(required - names)
        if missing:
            fail(f"Package is missing: {', '.join(missing)}")

        manifest = json.loads(archive.read(".mcp/server.json"))
        if manifest.get("version") != version:
            fail("Manifest version does not match package version")
        packages = manifest.get("packages") or []
        if not packages:
            fail("Manifest has no package declaration")
        declaration = packages[0]
        if declaration.get("identifier") != PACKAGE_ID:
            fail("Manifest package identifier is incorrect")
        if declaration.get("version") != version:
            fail("Manifest package version is incorrect")
        if declaration.get("transport", {}).get("type") != "stdio":
            fail("Manifest transport must be stdio")

        settings = archive.read("tools/net10.0/any/DotnetToolSettings.xml").decode("utf-8")
        if f'Name="{TOOL_COMMAND}"' not in settings:
            fail(f"Tool command must be {TOOL_COMMAND}")


def send(process: subprocess.Popen[str], message: dict[str, object]) -> None:
    assert process.stdin is not None
    process.stdin.write(json.dumps(message, separators=(",", ":")) + "\n")
    process.stdin.flush()


def wait_for(messages: queue.Queue[dict[str, object]], response_id: int, stderr: list[str], timeout: float = 90) -> dict[str, object]:
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        try:
            message = messages.get(timeout=0.5)
        except queue.Empty:
            continue
        if message.get("id") == response_id:
            return message
    fail(f"Timed out waiting for MCP response {response_id}: {''.join(stderr)}")


def smoke_test(directory: Path, version: str) -> None:
    dnx = shutil.which("dnx")
    if dnx is None:
        fail("dnx was not found; .NET 10 SDK is required")

    messages: queue.Queue[dict[str, object]] = queue.Queue()
    stderr: list[str] = []

    with tempfile.TemporaryDirectory(prefix="local-coding-mcp-dnx-") as temp:
        workspace = Path(temp) / "workspace"
        workspace.mkdir()
        env = os.environ.copy()
        env.update({
            "DOTNET_CLI_HOME": str(Path(temp) / "dotnet-home"),
            "DOTNET_NOLOGO": "1",
            "DOTNET_SKIP_FIRST_TIME_EXPERIENCE": "1",
            "NUGET_PACKAGES": str(Path(temp) / "packages"),
            "AllowedRoots__0": str(workspace),
            "ExecutionHistory__FilePath": str(Path(temp) / "history.jsonl"),
            "Skills__Directory": str(Path(temp) / "skills"),
        })

        process = subprocess.Popen(
            [dnx, f"{PACKAGE_ID}@{version}", "--source", str(directory.resolve()), "--verbosity", "quiet", "--yes"],
            stdin=subprocess.PIPE,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            encoding="utf-8",
            env=env,
        )

        def read_stdout() -> None:
            assert process.stdout is not None
            for line in process.stdout:
                line = line.strip()
                if not line:
                    continue
                try:
                    messages.put(json.loads(line))
                except json.JSONDecodeError:
                    messages.put({"invalid_stdout": line})

        def read_stderr() -> None:
            assert process.stderr is not None
            stderr.extend(process.stderr.readlines())

        threading.Thread(target=read_stdout, daemon=True).start()
        threading.Thread(target=read_stderr, daemon=True).start()

        try:
            send(process, {
                "jsonrpc": "2.0",
                "id": 1,
                "method": "initialize",
                "params": {
                    "protocolVersion": "2025-06-18",
                    "capabilities": {},
                    "clientInfo": {"name": "dnx-smoke-test", "version": "1.0"},
                },
            })
            initialized = wait_for(messages, 1, stderr)
            if "error" in initialized:
                fail(f"MCP initialize failed: {initialized['error']}")
            server_version = initialized.get("result", {}).get("serverInfo", {}).get("version")
            if server_version != version:
                fail(f"Server advertised {server_version}; expected {version}")

            send(process, {"jsonrpc": "2.0", "method": "notifications/initialized"})
            send(process, {"jsonrpc": "2.0", "id": 2, "method": "tools/list", "params": {}})
            tool_list = wait_for(messages, 2, stderr)
            tools = tool_list.get("result", {}).get("tools", [])
            names = {tool.get("name") for tool in tools if isinstance(tool, dict)}
            for required_tool in {"OpenWorkspace", "ReadFile", "RunCommand", "LoadEnabledSkills"}:
                if required_tool not in names:
                    fail(f"Packaged server did not advertise {required_tool}")
        finally:
            if process.stdin is not None:
                process.stdin.close()
            try:
                process.wait(timeout=10)
            except subprocess.TimeoutExpired:
                process.terminate()
                process.wait(timeout=5)


def main() -> int:
    if len(sys.argv) != 3:
        print("Usage: test-dnx-package.py <package-directory> <version>", file=sys.stderr)
        return 2

    directory = Path(sys.argv[1]).resolve()
    version = sys.argv[2]
    try:
        package = package_path(directory, version)
        validate_package(package, version)
        smoke_test(directory, version)
    except (OSError, RuntimeError, subprocess.SubprocessError, zipfile.BadZipFile) as exception:
        print(f"DNX package verification failed: {exception}", file=sys.stderr)
        return 1

    print(f"Validated and exercised {package.name} through dnx.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
