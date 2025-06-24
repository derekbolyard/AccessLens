import { environment } from '../environments/environment';

/**
 * Utility functions for working with Google Analytics
 */

/**
 * Sends a custom event to Google Analytics
 * @param eventName Name of the event
 * @param params Event parameters
 */
export function sendGAEvent(
  eventName: string,
  params: Record<string, any> = {}
): void {
  if (!environment.features.enableAnalytics || !window.gtag) return;
  
  window.gtag('event', eventName, params);
}

/**
 * Sends a page view event to Google Analytics
 * @param pagePath Path of the page
 * @param pageTitle Title of the page
 */
export function sendGAPageView(
  pagePath: string,
  pageTitle?: string
): void {
  if (!environment.features.enableAnalytics || !window.gtag) return;
  
  window.gtag('event', 'page_view', {
    page_path: pagePath,
    page_title: pageTitle || document.title
  });
}

/**
 * Sets user properties in Google Analytics
 * @param properties User properties to set
 */
export function setGAUserProperties(
  properties: Record<string, string>
): void {
  if (!environment.features.enableAnalytics || !window.gtag) return;
  
  window.gtag('set', 'user_properties', properties);
}

/**
 * Tracks an exception in Google Analytics
 * @param description Exception description
 * @param fatal Whether the exception was fatal
 */
export function trackGAException(
  description: string,
  fatal: boolean = false
): void {
  if (!environment.features.enableAnalytics || !window.gtag) return;
  
  window.gtag('event', 'exception', {
    description,
    fatal
  });
}

/**
 * Tracks a timing event in Google Analytics
 * @param name Name of the timing event
 * @param value Timing value in milliseconds
 * @param category Timing category
 * @param label Timing label
 */
export function trackGATiming(
  name: string,
  value: number,
  category: string,
  label?: string
): void {
  if (!environment.features.enableAnalytics || !window.gtag) return;
  
  window.gtag('event', 'timing_complete', {
    name,
    value,
    event_category: category,
    event_label: label
  });
}

/**
 * Measures and tracks the performance of a function
 * @param fn Function to measure
 * @param category Timing category
 * @param name Timing name
 */
export function measurePerformance<T extends (...args: any[]) => any>(
  fn: T,
  category: string,
  name: string
): (...args: Parameters<T>) => ReturnType<T> {
  return function(...args: Parameters<T>): ReturnType<T> {
    const start = performance.now();
    const result = fn(...args);
    const end = performance.now();
    
    trackGATiming(name, end - start, category);
    
    return result;
  };
}