const fs = require('fs');
const path = require('path');

// Import constants from TypeScript (simplified for Node.js)
const BUNDLE_LIMITS = {
  'main': 300 * 1024,      // 300KB for main bundle
  'polyfills': 80 * 1024,   // 80KB for polyfills
  'styles': 35 * 1024,      // 35KB for styles
  'vendor': 800 * 1024,     // 800KB for vendor bundle
};

const TOTAL_LIMIT = 1500 * 1024; // 1.5MB total limit

function checkBundleSize() {
  const statsPath = path.join(__dirname, '../dist/demo/stats.json');
  
  if (!fs.existsSync(statsPath)) {
    console.error('❌ Stats file not found. Run "ng build --stats-json" first.');
    process.exit(1);
  }

  const stats = JSON.parse(fs.readFileSync(statsPath, 'utf8'));
  const assets = stats.assets || [];
  
  let hasViolations = false;
  
  console.log('📊 Bundle Size Analysis\n');
  
  assets.forEach(asset => {
    const name = asset.name;
    const size = asset.size;
    
    // Check against limits
    Object.keys(BUNDLE_LIMITS).forEach(bundleName => {
      if (name.includes(bundleName) && name.endsWith('.js')) {
        const limit = BUNDLE_LIMITS[bundleName];
        const sizeKB = Math.round(size / 1024);
        const limitKB = Math.round(limit / 1024);
        
        if (size > limit) {
          console.log(`❌ ${name}: ${sizeKB}KB (exceeds ${limitKB}KB limit)`);
          hasViolations = true;
        } else {
          console.log(`✅ ${name}: ${sizeKB}KB (within ${limitKB}KB limit)`);
        }
      }
    });
  });
  
  // Calculate total bundle size
  const totalSize = assets
    .filter(asset => asset.name.endsWith('.js'))
    .reduce((total, asset) => total + asset.size, 0);
  
  const totalSizeKB = Math.round(totalSize / 1024);
  const totalLimit = TOTAL_LIMIT / 1024; // Convert to KB for display
  
  console.log(`\n📦 Total Bundle Size: ${totalSizeKB}KB`);
  
  if (totalSizeKB > totalLimit) {
    console.log(`❌ Total bundle size exceeds ${totalLimit}KB limit`);
    hasViolations = true;
  } else {
    console.log(`✅ Total bundle size within ${totalLimit}KB limit`);
  }
  
  if (hasViolations) {
    console.log('\n🚨 Bundle size violations detected!');
    console.log('Consider:');
    console.log('- Lazy loading more routes');
    console.log('- Tree shaking unused code');
    console.log('- Splitting large components');
    console.log('- Using dynamic imports');
    process.exit(1);
  } else {
    console.log('\n🎉 All bundle sizes are within limits!');
  }
}

checkBundleSize();