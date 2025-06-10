const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

function runPerformanceAudit() {
  console.log('⚡ Running Performance Audit\n');
  
  const results = {
    bundleSize: false,
    lighthouse: false,
    buildTime: false,
    memoryUsage: false
  };
  
  // 1. Bundle size analysis
  try {
    console.log('📦 Analyzing bundle size...');
    const startTime = Date.now();
    execSync('ng build --stats-json', { stdio: 'inherit' });
    const buildTime = Date.now() - startTime;
    
    // Check bundle sizes
    const statsPath = './dist/demo/stats.json';
    if (fs.existsSync(statsPath)) {
      const stats = JSON.parse(fs.readFileSync(statsPath, 'utf8'));
      const assets = stats.assets || [];
      
      const totalSize = assets
        .filter(asset => asset.name.endsWith('.js'))
        .reduce((total, asset) => total + asset.size, 0);
      
      const totalSizeKB = Math.round(totalSize / 1024);
      
      console.log(`📊 Total bundle size: ${totalSizeKB}KB`);
      console.log(`⏱️  Build time: ${buildTime}ms`);
      
      if (totalSizeKB < 2000 && buildTime < 60000) { // 2MB and 60s limits
        results.bundleSize = true;
        results.buildTime = true;
        console.log('✅ Bundle size and build time within limits\n');
      } else {
        console.log('❌ Bundle size or build time exceeds limits\n');
      }
    }
  } catch (error) {
    console.log('❌ Failed to analyze bundle size\n');
  }
  
  // 2. Lighthouse audit (if server is running)
  try {
    console.log('🔍 Running Lighthouse audit...');
    
    // Check if server is running
    try {
      execSync('curl -f http://localhost:4200 > /dev/null 2>&1');
      
      // Run Lighthouse
      execSync('npx lighthouse http://localhost:4200 --output json --output-path ./reports/lighthouse.json --chrome-flags="--headless"', { stdio: 'inherit' });
      
      const lighthouseReport = JSON.parse(fs.readFileSync('./reports/lighthouse.json', 'utf8'));
      const scores = lighthouseReport.lhr.categories;
      
      console.log('📊 Lighthouse Scores:');
      console.log(`  Performance: ${Math.round(scores.performance.score * 100)}`);
      console.log(`  Accessibility: ${Math.round(scores.accessibility.score * 100)}`);
      console.log(`  Best Practices: ${Math.round(scores['best-practices'].score * 100)}`);
      console.log(`  SEO: ${Math.round(scores.seo.score * 100)}`);
      
      if (scores.performance.score >= 0.9 && scores.accessibility.score >= 0.9) {
        results.lighthouse = true;
        console.log('✅ Lighthouse audit passed\n');
      } else {
        console.log('❌ Lighthouse audit failed minimum thresholds\n');
      }
    } catch (serverError) {
      console.log('⚠️  Server not running, skipping Lighthouse audit\n');
    }
  } catch (error) {
    console.log('❌ Failed to run Lighthouse audit\n');
  }
  
  // 3. Memory usage analysis
  try {
    console.log('🧠 Analyzing memory usage...');
    
    // Run tests to check for memory leaks
    execSync('ng test --watch=false --browsers=ChromeHeadless --include="**/performance/**/*.spec.ts"', { stdio: 'inherit' });
    
    results.memoryUsage = true;
    console.log('✅ Memory usage tests passed\n');
  } catch (error) {
    console.log('❌ Memory usage tests failed\n');
  }
  
  // Generate performance report
  const reportDir = './reports';
  if (!fs.existsSync(reportDir)) {
    fs.mkdirSync(reportDir, { recursive: true });
  }
  
  const report = {
    timestamp: new Date().toISOString(),
    results,
    summary: {
      passed: Object.values(results).filter(Boolean).length,
      total: Object.keys(results).length
    }
  };
  
  fs.writeFileSync(
    path.join(reportDir, 'performance-audit.json'),
    JSON.stringify(report, null, 2)
  );
  
  // Performance recommendations
  console.log('💡 Performance Recommendations:');
  
  if (!results.bundleSize) {
    console.log('  - Consider lazy loading more routes');
    console.log('  - Implement tree shaking for unused code');
    console.log('  - Split large components into smaller ones');
  }
  
  if (!results.lighthouse) {
    console.log('  - Optimize images and assets');
    console.log('  - Implement service worker for caching');
    console.log('  - Minimize render-blocking resources');
  }
  
  if (!results.memoryUsage) {
    console.log('  - Fix memory leaks in components');
    console.log('  - Implement proper subscription cleanup');
    console.log('  - Optimize data structures and algorithms');
  }
  
  // Summary
  console.log('\n📊 Performance Audit Summary');
  console.log(`✅ Passed: ${report.summary.passed}/${report.summary.total} checks`);
  
  if (report.summary.passed === report.summary.total) {
    console.log('🎉 All performance checks passed!');
    process.exit(0);
  } else {
    console.log('⚠️  Some performance checks need attention. Review recommendations above.');
    process.exit(1);
  }
}

runPerformanceAudit();