export const environment = {
  production: false,
  apiUrl: 'https://localhost:7088/api',
  supportEmail: 'support@accessibilityreports.com',
  features: {
    enableAnalytics: false,
    enableErrorReporting: false,
    maxFileUploadSize: 10 * 1024 * 1024,
    scanTimeout: 300000,
    // New auth feature toggle
    useMagicLinkAuth: true, // Set to true to use new magic link flow, false for old OAuth
    // New mock services toggle
    useMocks: true // Set to true to use mock services instead of real API calls
  }
};