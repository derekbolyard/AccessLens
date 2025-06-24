/**
 * This file contains utility functions that help with tree-shaking
 * by ensuring imports are specific and granular.
 */

/**
 * Creates a proxy for importing only specific parts of a module
 * @param module The module to create a proxy for
 * @param imports The specific imports to include
 */
export function createModuleProxy<T extends object>(module: T, imports: (keyof T)[]): Partial<T> {
  const proxy: Partial<T> = {};
  
  for (const key of imports) {
    if (key in module) {
      proxy[key] = module[key];
    }
  }
  
  return proxy;
}

/**
 * Dynamically imports a module only when needed
 * @param importFn Function that returns a dynamic import
 */
export async function lazyImport<T>(importFn: () => Promise<T>): Promise<T> {
  try {
    return await importFn();
  } catch (error) {
    console.error('Failed to lazy load module:', error);
    throw error;
  }
}

/**
 * Checks if a feature is being used to conditionally import code
 * @param featureCheck Function that returns whether the feature is used
 * @param importFn Function that returns a dynamic import
 */
export async function conditionalImport<T>(
  featureCheck: () => boolean,
  importFn: () => Promise<T>
): Promise<T | null> {
  if (featureCheck()) {
    return await importFn();
  }
  return null;
}

/**
 * Utility to help with importing only what's needed from RxJS
 * @param operators List of RxJS operators to import
 */
export function importRxJSOperators<T extends string>(operators: T[]): Record<T, any> {
  // This is just a type helper - the actual importing happens elsewhere
  return {} as Record<T, any>;
}