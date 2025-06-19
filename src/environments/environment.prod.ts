export const environment = {
  production: true,
  apiUrl: 'https://your-api-domain.com/api',
  supportEmail: 'support@accessibilityreports.com',
  features: {
    enableAnalytics: true,
    enableErrorReporting: true,
    maxFileUploadSize: 10 * 1024 * 1024,
    scanTimeout: 300000,
    // For production, you can control this toggle
    useMagicLinkAuth: false, // Start with false for gradual rollout
    // In production, we always use real services
    useMocks: false
  }
};