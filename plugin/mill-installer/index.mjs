#!/usr/bin/env node
/**
 * mill-installer MCP Server
 *
 * Auto-installs the mill CLI when the plugin is enabled.
 */

import { execSync, spawn } from "child_process";
import { existsSync, mkdirSync, chmodSync, createWriteStream } from "fs";
import { homedir, platform, arch } from "os";
import { join } from "path";
import https from "https";
import readline from "readline";

const isWindows = platform() === "win32";
const isMac = platform() === "darwin";
const isLinux = platform() === "linux";

// Installation paths
const installDir = isWindows
  ? join(process.env.LOCALAPPDATA || join(homedir(), "AppData", "Local"), "Programs", "mill")
  : join(homedir(), ".local", "bin");

const binaryPath = isWindows
  ? join(installDir, "mill.exe")
  : join(installDir, "mill");

function log(msg) {
  process.stderr.write(`[mill-installer] ${msg}\n`);
}

/**
 * Check if mill CLI is installed
 */
function isMillInstalled() {
  if (existsSync(binaryPath)) return true;
  try {
    execSync(isWindows ? "where mill" : "which mill", { stdio: "ignore" });
    return true;
  } catch {
    return false;
  }
}

/**
 * Download file from URL, following redirects
 */
function download(url, dest) {
  return new Promise((resolve, reject) => {
    const follow = (url) => {
      https.get(url, (res) => {
        if (res.statusCode >= 300 && res.statusCode < 400 && res.headers.location) {
          follow(res.headers.location);
          return;
        }
        if (res.statusCode !== 200) {
          reject(new Error(`HTTP ${res.statusCode}`));
          return;
        }
        const file = createWriteStream(dest);
        res.pipe(file);
        file.on("finish", () => { file.close(); resolve(); });
        file.on("error", reject);
      }).on("error", reject);
    };
    follow(url);
  });
}

/**
 * Get latest release from GitHub
 */
function getLatestRelease() {
  return new Promise((resolve, reject) => {
    https.get("https://api.github.com/repos/mindrevolution/mill/releases/latest", {
      headers: { "User-Agent": "mill-installer" }
    }, (res) => {
      let data = "";
      res.on("data", (chunk) => data += chunk);
      res.on("end", () => {
        try { resolve(JSON.parse(data)); }
        catch (e) { reject(e); }
      });
      res.on("error", reject);
    }).on("error", reject);
  });
}

/**
 * Install mill CLI
 */
async function installMill() {
  log("Installing mill CLI...");

  const cpuArch = arch() === "arm64" ? "arm64" : "x64";
  let assetName;
  if (isWindows) assetName = `mill-win-${cpuArch}.exe`;
  else if (isMac) assetName = `mill-osx-${cpuArch}`;
  else if (isLinux) assetName = `mill-linux-${cpuArch}`;
  else throw new Error(`Unsupported platform: ${platform()}`);

  try {
    const release = await getLatestRelease();
    const asset = release.assets?.find(a => a.name === assetName);
    if (!asset) throw new Error(`No asset: ${assetName}`);

    mkdirSync(installDir, { recursive: true });
    log(`Downloading ${assetName}...`);
    await download(asset.browser_download_url, binaryPath);

    if (!isWindows) chmodSync(binaryPath, 0o755);
    log(`Installed to ${binaryPath}`);

    const pathEnv = process.env.PATH || "";
    if (!pathEnv.split(isWindows ? ";" : ":").includes(installDir)) {
      log(`Note: Add ${installDir} to your PATH`);
    }
    return true;
  } catch (error) {
    log(`Installation failed: ${error.message}`);
    return false;
  }
}

/**
 * Minimal MCP server using raw JSON-RPC over stdio
 */
async function runMcpServer() {
  const rl = readline.createInterface({ input: process.stdin });

  const respond = (id, result) => {
    const msg = JSON.stringify({ jsonrpc: "2.0", id, result });
    process.stdout.write(`Content-Length: ${Buffer.byteLength(msg)}\r\n\r\n${msg}`);
  };

  const respondError = (id, code, message) => {
    const msg = JSON.stringify({ jsonrpc: "2.0", id, error: { code, message } });
    process.stdout.write(`Content-Length: ${Buffer.byteLength(msg)}\r\n\r\n${msg}`);
  };

  let buffer = "";

  rl.on("line", (line) => {
    buffer += line + "\n";

    // Check for complete message (after empty line following headers)
    if (buffer.includes("\r\n\r\n") || buffer.includes("\n\n")) {
      try {
        const parts = buffer.split(/\r?\n\r?\n/);
        if (parts.length >= 2) {
          const body = parts.slice(1).join("\n\n").trim();
          if (body) {
            const request = JSON.parse(body);
            handleRequest(request.id, request.method, request.params);
            buffer = "";
          }
        }
      } catch (e) {
        // Continue buffering
      }
    }
  });

  function handleRequest(id, method, params) {
    switch (method) {
      case "initialize":
        respond(id, {
          protocolVersion: "2024-11-05",
          capabilities: { tools: {} },
          serverInfo: { name: "mill-installer", version: "1.0.0" }
        });
        break;

      case "notifications/initialized":
        // No response needed
        break;

      case "tools/list":
        respond(id, {
          tools: [{
            name: "mill_status",
            description: "Check mill CLI installation status",
            inputSchema: { type: "object", properties: {} }
          }]
        });
        break;

      case "tools/call":
        if (params?.name === "mill_status") {
          respond(id, {
            content: [{
              type: "text",
              text: JSON.stringify({
                installed: isMillInstalled(),
                path: binaryPath,
                platform: platform(),
                arch: arch()
              }, null, 2)
            }]
          });
        } else {
          respondError(id, -32601, `Unknown tool: ${params?.name}`);
        }
        break;

      default:
        if (id !== undefined) {
          respondError(id, -32601, `Method not found: ${method}`);
        }
    }
  }
}

// Main
(async () => {
  if (!isMillInstalled()) {
    await installMill();
  } else {
    log("mill CLI already installed");
  }

  await runMcpServer();
})();
