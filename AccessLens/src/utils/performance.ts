/**
 * Performance utility functions for optimizing Angular applications
 */

/**
 * Debounces a function to limit how often it can be called
 * @param func The function to debounce
 * @param wait The time to wait in milliseconds
 */
export function debounce<T extends (...args: any[]) => any>(
  func: T,
  wait: number
): (...args: Parameters<T>) => void {
  let timeout: number | null = null;
  
  return function(...args: Parameters<T>): void {
    const later = () => {
      timeout = null;
      func(...args);
    };
    
    if (timeout !== null) {
      clearTimeout(timeout);
    }
    timeout = window.setTimeout(later, wait) as unknown as number;
  };
}

/**
 * Throttles a function to limit how often it can be called
 * @param func The function to throttle
 * @param limit The time limit in milliseconds
 */
export function throttle<T extends (...args: any[]) => any>(
  func: T,
  limit: number
): (...args: Parameters<T>) => void {
  let inThrottle = false;
  
  return function(...args: Parameters<T>): void {
    if (!inThrottle) {
      func(...args);
      inThrottle = true;
      setTimeout(() => {
        inThrottle = false;
      }, limit);
    }
  };
}

/**
 * Memoizes a function to cache its results
 * @param func The function to memoize
 */
export function memoize<T extends (...args: any[]) => any>(
  func: T
): (...args: Parameters<T>) => ReturnType<T> {
  const cache = new Map<string, ReturnType<T>>();
  
  return function(...args: Parameters<T>): ReturnType<T> {
    const key = JSON.stringify(args);
    
    if (cache.has(key)) {
      return cache.get(key) as ReturnType<T>;
    }
    
    const result = func(...args);
    cache.set(key, result);
    return result;
  };
}

/**
 * Measures the execution time of a function
 * @param func The function to measure
 * @param label A label for the console output
 */
export function measurePerformance<T extends (...args: any[]) => any>(
  func: T,
  label: string
): (...args: Parameters<T>) => ReturnType<T> {
  return function(...args: Parameters<T>): ReturnType<T> {
    const start = performance.now();
    const result = func(...args);
    const end = performance.now();
    
    console.log(`${label} took ${end - start}ms`);
    
    return result;
  };
}

/**
 * Creates a function that will only execute once the browser is idle
 * @param func The function to run during idle time
 */
export function runWhenIdle<T extends (...args: any[]) => any>(
  func: T
): (...args: Parameters<T>) => void {
  return function(...args: Parameters<T>): void {
    if ('requestIdleCallback' in window) {
      (window as any).requestIdleCallback(() => {
        func(...args);
      });
    } else {
      setTimeout(() => {
        func(...args);
      }, 1);
    }
  };
}

/**
 * Lazy loads an image with IntersectionObserver
 * @param imageElement The image element to lazy load
 * @param src The source URL of the image
 */
export function lazyLoadImage(imageElement: HTMLImageElement, src: string): void {
  if ('IntersectionObserver' in window) {
    const observer = new IntersectionObserver((entries) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          imageElement.src = src;
          observer.disconnect();
        }
      });
    });
    
    observer.observe(imageElement);
  } else {
    // Fallback for browsers that don't support IntersectionObserver
    imageElement.src = src;
  }
}