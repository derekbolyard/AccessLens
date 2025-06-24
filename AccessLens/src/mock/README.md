# Mock Backend Services

This folder contains all the mock/fake backend services and data used for development and testing. These files simulate real API calls and provide realistic data without requiring an actual backend.

## 📁 Files Overview

### `mock-backend.service.ts`
- **Purpose**: Main mock service that simulates all API endpoints
- **Features**: 
  - Complete CRUD operations for sites, reports, pages, and issues
  - Realistic API delays and error simulation
  - Authentication flow simulation
  - Data persistence during session

### `mock-data.ts`
- **Purpose**: Static mock data definitions
- **Contains**:
  - Sample sites (3 different websites)
  - Sample reports (6 reports across different sites)
  - Sample pages (5 pages with different accessibility scores)
  - Sample accessibility issues (8 realistic WCAG violations)

### `mock-auth.service.ts`
- **Purpose**: Authentication-specific mock service
- **Features**:
  - Magic link email verification simulation
  - OAuth (Google/GitHub) simulation
  - CSRF token generation
  - User session management

## 🔧 How to Use

### Enable Mock Backend
In your environment files:

```typescript
// src/environments/environment.ts (development)
export const environment = {
  // ... other config
  features: {
    useMockBackend: true, // Enable mock backend
  }
};

// src/environments/environment.prod.ts (production)
export const environment = {
  // ... other config
  features: {
    useMockBackend: false, // Use real backend
  }
};
```

### Testing Authentication
- **Magic Link**: Use verification code `123456` for successful login
- **OAuth**: Both Google and GitHub will return mock users

### Mock Data Structure
- **3 Sites**: Corporate Website, E-commerce Store, Blog Platform
- **6 Reports**: Various completion statuses and scores
- **5 Pages**: Different accessibility scores (85-95%)
- **8 Issues**: Mix of errors, warnings, and notices

## 🚀 Benefits

1. **No Backend Dependency**: Develop frontend without waiting for backend
2. **Realistic Testing**: Proper delays and error scenarios
3. **Easy Switching**: Toggle between mock and real backend
4. **Consistent Data**: Predictable test data for development
5. **Offline Development**: Work without internet connection

## 🔄 Switching to Real Backend

When your backend is ready:

1. Set `useMockBackend: false` in environment
2. Update API URLs in environment files
3. The `ApiService` will automatically route to real endpoints
4. Remove or exclude this mock folder from production builds

## 📝 Adding New Mock Data

To add new mock data:

1. Add new entries to the arrays in `mock-data.ts`
2. Update the `initializeMockData()` method in `mock-backend.service.ts`
3. Ensure proper relationships between entities (sites → reports → pages → issues)

## 🧪 Testing Scenarios

The mock backend includes various scenarios for testing:

- **Successful operations**: Most API calls succeed
- **Error simulation**: ~10% random failure rate (configurable)
- **Loading states**: Realistic delays (300-1300ms)
- **Different statuses**: In-progress, completed, and failed states
- **Data relationships**: Proper parent-child relationships

## 🗂️ File Structure

```
src/mock/
├── mock-backend.service.ts    # Main mock API service
├── mock-data.ts              # Static mock data
├── mock-auth.service.ts      # Authentication mocks
└── README.md                 # This documentation
```

This mock system provides a complete development environment that closely mimics a real backend, allowing for efficient frontend development and testing.