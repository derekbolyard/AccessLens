/**
 * This script detects the backend API port used by the .NET application
 * when launched through VS Code or other means, with fallback to default port.
 */
const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

// Helper to check if a port is in use
function isPortInUse(port) {
  try {
    // On Windows, check if the port is in use
    const result = execSync(`powershell "Get-NetTCPConnection -LocalPort ${port} -ErrorAction SilentlyContinue | Select-Object -First 1"`, { stdio: 'pipe' }).toString();
    return result.trim() !== '';
  } catch (e) {
    return false;
  }
}

// Try to detect which ports are active
function detectApiPort() {
  const possiblePorts = [7088, 5261, 44389, 42462, 5001, 5000];
  
  // First, check if any HTTPS ports are active (prefer HTTPS)
  for (const port of possiblePorts) {
    if (isPortInUse(port)) {
      console.log(`✅ Detected backend API running on port ${port}`);
      return port;
    }
  }
  
  // If no port detected, use the default
  console.log('⚠️ Could not detect backend API port, using default port: 7088');
  return 7088;
}

const apiPort = detectApiPort();
const isHttps = [7088, 44389, 5001].includes(apiPort);
const apiProtocol = isHttps ? 'https' : 'http';

/**
 * Angular dev proxy configuration with dynamic port detection
 */
module.exports = {
  '/api': {
    target: `${apiProtocol}://localhost:${apiPort}`,
    secure: false,
    changeOrigin: true,
    logLevel: 'debug',
    cookieDomainRewrite: 'localhost',
    cookiePathRewrite: { '^/api': '/' }
  }
};

console.log(`🔌 Proxying API requests to: ${apiProtocol}://localhost:${apiPort}`);
