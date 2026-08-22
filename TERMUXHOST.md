# Run LocalCodingMcp on TermuxHost

LocalCodingMcp can run natively on Termux with the .NET 10 runtime installed by TermuxHost. Docker is not required.

## Release package

The `Release ZIP` GitHub Actions workflow publishes a framework-dependent package:

```text
local-coding-mcp-termux-aarch64.zip
local-coding-mcp-termux-aarch64.zip.sha256
```

The ZIP contains the output of:

```bash
dotnet publish LocalCodingMcp/LocalCodingMcp.csproj \
  -c Release \
  --self-contained false
```

It is intentionally framework-dependent so it uses the .NET 10 runtime already installed in Termux instead of bundling a Linux runtime.

## Create a release

Create and push a version tag:

```bash
git tag v0.1.0
git push origin v0.1.0
```

GitHub Actions will:

1. restore the .NET project;
2. publish `LocalCodingMcp` in Release mode;
3. start the published DLL on port `5000`;
4. verify `GET /health`;
5. create the Termux ZIP and SHA-256 checksum;
6. create/update the GitHub Release and attach the tested files.

You can also run the workflow manually from GitHub Actions. A manual run produces downloadable workflow artifacts but does not create a GitHub Release.

## TermuxHost application settings

Extract the release ZIP to a directory such as:

```text
/data/data/com.termux/files/home/hosting/apps/local-coding-mcp/current
```

Create an application in TermuxHost with:

```text
ID: local-coding-mcp
Name: Local Coding MCP
Port: 5000
Working directory: /data/data/com.termux/files/home/hosting/apps/local-coding-mcp/current
Startup DLL: LocalCodingMcp.dll
Auto start: enabled
```

Environment/settings:

```text
ASPNETCORE_URLS=http://0.0.0.0:5000
AllowedRoots__0=/data/data/com.termux/files/home/workspaces
CommandTimeoutSeconds=30
```

Create the allowed workspace root once:

```bash
mkdir -p ~/workspaces
```

Then start the application and verify:

```bash
curl http://127.0.0.1:5000/health
```

Expected response:

```json
{"status":"ok"}
```

The MCP endpoint is:

```text
http://<PHONE-LAN-IP>:5000/mcp
```

For remote MCP clients, expose port `5000` with the TermuxHost ngrok feature and use:

```text
https://<your-ngrok-host>/mcp
```

## Security

`AllowedRoots` controls which directories MCP tools may access. Keep it limited to dedicated workspace directories. Do not point it at your entire Termux home directory unless that is explicitly intended.
