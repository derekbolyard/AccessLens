import 'zone.js/testing';
import { getTestBed } from '@angular/core/testing';
import {
  BrowserTestingModule,
  platformBrowserTesting
} from '@angular/platform-browser/testing';

// First, initialize the Angular testing environment with the new modules
getTestBed().initTestEnvironment(
  BrowserTestingModule,
  platformBrowserTesting()
);

// Add app-root element to DOM for integration tests
if (!document.querySelector('app-root')) {
  const appRoot = document.createElement('app-root');
  document.body.appendChild(appRoot);
};

// Create a simple test inline to verify setup works
describe('Basic Test Setup', () => {
  it('should work', () => {
    expect(true).toBe(true);
  });
  
  afterAll(() => {
    // Clean up the test environment
    getTestBed().resetTestEnvironment();
  });
});