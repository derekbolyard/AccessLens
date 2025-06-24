import * as Sentry from '@sentry/angular';
import { environment } from '../environments/environment';

/**
 * Utility functions for working with Sentry
 */

/**
 * Captures an exception in Sentry with additional context
 * @param error The error to capture
 * @param context Additional context information
 */
export function captureException(error: any, context?: Record<string, any>): void {
  if (!environment.features.enableErrorReporting) return;
  
  Sentry.captureException(error, {
    contexts: context ? { additionalContext: context } : undefined
  });
}

/**
 * Captures a message in Sentry
 * @param message The message to capture
 * @param level The severity level
 */
export function captureMessage(message: string, level: 'fatal' | 'error' | 'warning' | 'log' | 'info' | 'debug' = 'info'): void {
  if (!environment.features.enableErrorReporting) return;
  
  Sentry.captureMessage(message, level);
}

/**
 * Adds breadcrumb to the current Sentry scope
 * @param message Breadcrumb message
 * @param category Breadcrumb category
 * @param data Additional data
 */
export function addBreadcrumb(
  message: string, 
  category: string = 'custom', 
  data?: Record<string, any>
): void {
  if (!environment.features.enableErrorReporting) return;
  
  Sentry.addBreadcrumb({
    message,
    category,
    data,
    level: 'info'
  });
}

/**
 * Starts a new transaction for performance monitoring
 * @param name Transaction name
 * @param op Operation type
 */
export function startTransaction(name: string, op: string): any {
  if (!environment.features.enableErrorReporting) return undefined;
  
  return Sentry.startSpan({ name, op }, () => {
    // Transaction logic would go here
  });
}

/**
 * Sets a tag on the current Sentry scope
 * @param key Tag key
 * @param value Tag value
 */
export function setTag(key: string, value: string): void {
  if (!environment.features.enableErrorReporting) return;
  
  Sentry.setTag(key, value);
}

/**
 * Wraps a function with Sentry monitoring
 * @param fn Function to wrap
 * @param name Operation name
 */
export function withSentryMonitoring<T extends (...args: any[]) => any>(
  fn: T,
  name: string
): (...args: Parameters<T>) => ReturnType<T> {
  return function(...args: Parameters<T>): ReturnType<T> {
    if (!environment.features.enableErrorReporting) {
      return fn(...args);
    }
    
    return Sentry.startSpan({ name, op: 'function' }, () => {
      try {
        return fn(...args);
      } catch (error) {
        captureException(error, { functionName: name, arguments: args });
        throw error;
      }
    });
  };
}