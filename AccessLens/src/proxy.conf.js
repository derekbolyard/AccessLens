/**
 * Angular dev proxy — sends /api/* to the .NET Kestrel dev port.
 * Run `ng serve --proxy-config proxy.conf.js`
 */
module.exports = {
  '/api': {
    target: 'https://localhost:7088',   // ← your backend URL/port
    secure: false,                      // ignore self-signed HTTPS cert in dev
    changeOrigin: true,                 // rewrites Host header → backend
    logLevel: 'debug',

    // Make cookie-based auth work on port 4200:
    cookieDomainRewrite: 'localhost',   // Strip/replace Domain attribute
    cookiePathRewrite: { '^/api': '/' } // Optional: fix Path from backend
  }
};