export const environment = {
  production: true,
  apiUrl: 'https://accesslens.app/api',
  supportEmail: 'support@getaccesslens.com',
  features: {
    enableAnalytics: true,
    enableErrorReporting: true,
    maxFileUploadSize: 10 * 1024 * 1024,
    scanTimeout: 300000,
    useMagicLinkAuth: true,
  }
};