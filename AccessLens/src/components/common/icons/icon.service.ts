import { Injectable } from '@angular/core';

export interface IconDefinition {
  viewBox: string;
  path: string;
}

@Injectable({
  providedIn: 'root'
})
export class IconService {
  private icons: Map<string, IconDefinition> = new Map([
    ['plus', { viewBox: '0 0 24 24', path: 'M12 5v14m-7-7h14' }],
    ['check', { viewBox: '0 0 24 24', path: 'l20 6-9 17-4-12' }],
    ['x', { viewBox: '0 0 24 24', path: 'M18 6L6 18M6 6l12 12' }],
    ['arrow-left', { viewBox: '0 0 24 24', path: 'M15 18l-6-6 6-6' }],
    ['arrow-right', { viewBox: '0 0 24 24', path: 'M9 6l6 6-6 6' }],
    ['external-link', { viewBox: '0 0 24 24', path: 'M18 13v6a2 2 0 01-2 2H5a2 2 0 01-2-2V8a2 2 0 012-2h6m4-3h6v6m-11 5L21 3' }],
    ['refresh', { viewBox: '0 0 24 24', path: 'M23 4v6h-6M1 20v-6h6m-3-6a9 9 0 019-9 9.75 9.75 0 016.74 2.74L21 4M3 20l2.26-2.26A9.75 9.75 0 0012 23a9 9 0 009-9' }],
    ['download', { viewBox: '0 0 24 24', path: 'M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4m7-10v12m-4-4l4 4 4-4' }],
    ['upload', { viewBox: '0 0 24 24', path: 'M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4m7-6v12m-4-4l4-4 4 4' }],
    ['edit', { viewBox: '0 0 24 24', path: 'M11 4H4a2 2 0 00-2 2v14a2 2 0 002 2h14a2 2 0 002-2v-7m-1.5-9.5a2.121 2.121 0 113 3L12 15l-4 1 1-4 9.5-9.5z' }],
    ['trash', { viewBox: '0 0 24 24', path: 'M3 6h18m-2 0v14a2 2 0 01-2 2H7a2 2 0 01-2-2V6m3 0V4a2 2 0 012-2h4a2 2 0 012 2v2m-6 5v6m4-6v6' }],
    ['eye', { viewBox: '0 0 24 24', path: 'M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8zm10 0a3 3 0 100-6 3 3 0 000 6z' }],
    ['settings', { viewBox: '0 0 24 24', path: 'M12 15a3 3 0 100-6 3 3 0 000 6zm9.5-3a9.5 9.5 0 11-19 0 9.5 9.5 0 0119 0z' }],
    ['search', { viewBox: '0 0 24 24', path: 'M11 19a8 8 0 100-16 8 8 0 000 16zm10 2l-4.35-4.35' }],
    ['email', { viewBox: '0 0 24 24', path: 'M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2zm16 2L12 13 4 6' }],
    ['lock', { viewBox: '0 0 24 24', path: 'M19 11H5a2 2 0 00-2 2v7a2 2 0 002 2h14a2 2 0 002-2v-7a2 2 0 00-2-2zM7 11V7a5 5 0 0110 0v4' }],
    ['user', { viewBox: '0 0 24 24', path: 'M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2m8-10a4 4 0 100-8 4 4 0 000 8z' }],
    ['phone', { viewBox: '0 0 24 24', path: 'M22 16.92v3a2 2 0 01-2.18 2 19.79 19.79 0 01-8.63-3.07 19.5 19.5 0 01-6-6 19.79 19.79 0 01-3.07-8.67A2 2 0 014.11 2h3a2 2 0 012 1.72c.127.96.361 1.903.7 2.81a2 2 0 01-.45 2.11L8.09 9.91a16 16 0 006 6l1.27-1.27a2 2 0 012.11-.45c.907.339 1.85.573 2.81.7A2 2 0 0122 16.92z' }],
    ['globe', { viewBox: '0 0 24 24', path: 'M12 22c5.523 0 10-4.477 10-10S17.523 2 12 2 2 6.477 2 12s4.477 10 10 10zm0-20v20M2 12h20' }],
    ['home', { viewBox: '0 0 24 24', path: 'M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2V9z' }],
    ['sites', { viewBox: '0 0 24 24', path: 'M21 16V8a2 2 0 00-1-1.73l-7-4a2 2 0 00-2 0l-7 4A2 2 0 003 8v8a2 2 0 001 1.73l7 4a2 2 0 002 0l7-4A2 2 0 0021 16z' }],
    ['reports', { viewBox: '0 0 24 24', path: 'M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8zm0 0l6 6' }],
    ['pages', { viewBox: '0 0 24 24', path: 'M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z' }],
    ['issues', { viewBox: '0 0 24 24', path: 'M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z' }],
    ['dashboard', { viewBox: '0 0 24 24', path: 'M3 3h7v7H3V3zm11 0h7v7h-7V3zM3 14h7v7H3v-7zm11 0h7v7h-7v-7z' }]
  ]);

  getIcon(name: string): IconDefinition | null {
    return this.icons.get(name) || null;
  }

  getAllIcons(): string[] {
    return Array.from(this.icons.keys());
  }
}