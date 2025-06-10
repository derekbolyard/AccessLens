export const environment = {
  production: false,
  apiUrl: 'https://localhost:7001/api', // Updated to match API port
  supportEmail: 'support@accessibilityreports.com',
  version: '1.0.0',
  features: {
    enableAnalytics: false,
    enableErrorReporting: false,
    maxFileUploadSize: 10 * 1024 * 1024, // 10MB
    scanTimeout: 300000, // 5 minutes
  }
};