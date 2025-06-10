export const environment = {
  production: true,
  apiUrl: 'https://api.accesslens.com/api', // Updated for production
  supportEmail: 'support@accessibilityreports.com',
  version: '1.0.0',
  features: {
    enableAnalytics: true,
    enableErrorReporting: true,
    maxFileUploadSize: 10 * 1024 * 1024, // 10MB
    scanTimeout: 300000, // 5 minutes
  }
};