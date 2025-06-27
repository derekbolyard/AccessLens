// Application Constants
// Consolidates magic numbers and hardcoded values

export const APP_CONSTANTS = {
  // Bundle Size Limits (in bytes)
  BUNDLE_LIMITS: {
    MAIN: 300 * 1024,      // 300KB for main bundle
    POLYFILLS: 80 * 1024,  // 80KB for polyfills  
    STYLES: 35 * 1024,     // 35KB for styles
    VENDOR: 800 * 1024,    // 800KB for vendor bundle
    TOTAL: 1500 * 1024,    // 1.5MB total limit
  },

  // Performance Thresholds
  PERFORMANCE: {
    LOAD_TIME_THRESHOLD: 3000,        // 3 seconds
    FIRST_PAINT_THRESHOLD: 1000,      // 1 second
    LIGHTHOUSE_MIN_SCORE: 90,         // Minimum Lighthouse score
  },

  // API Configuration
  API: {
    REQUEST_TIMEOUT: 30000,           // 30 seconds
    RETRY_ATTEMPTS: 3,
    RETRY_DELAY: 1000,                // 1 second
  },

  // UI Configuration  
  UI: {
    DEBOUNCE_DELAY: 300,              // 300ms for search inputs
    ANIMATION_DURATION: 200,          // 200ms for transitions
    PAGINATION_SIZE: 20,              // Items per page
    MAX_FILE_SIZE: 10 * 1024 * 1024,  // 10MB max file upload
  },

  // Accessibility
  A11Y: {
    MIN_CONTRAST_RATIO: 4.5,
    MIN_LARGE_TEXT_CONTRAST: 3.0,
    FOCUS_OUTLINE_WIDTH: 3,           // 3px focus outline
  },

  // Local Storage Keys
  STORAGE_KEYS: {
    USER_PREFERENCES: 'accesslens_user_preferences',
    THEME: 'accesslens_theme',
    RECENT_SCANS: 'accesslens_recent_scans',
  },

  // Error Messages
  ERRORS: {
    NETWORK_ERROR: 'Network connection failed. Please try again.',
    FILE_TOO_LARGE: 'File size exceeds maximum limit.',
    INVALID_URL: 'Please enter a valid URL.',
    SCAN_FAILED: 'Accessibility scan failed. Please try again.',
  },
} as const;

// Type for bundle limits
export type BundleLimits = typeof APP_CONSTANTS.BUNDLE_LIMITS;

// Export individual constants for convenience
export const { BUNDLE_LIMITS, PERFORMANCE, API, UI, A11Y, STORAGE_KEYS, ERRORS } = APP_CONSTANTS;
