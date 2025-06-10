const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

function runSecurityAudit() {
  console.log('🔒 Running Security Audit\n');
  
  const results = {
    npmAudit: false,
    retireJs: false,
    dependencyCheck: false,
    codeAnalysis: false
  };
  
  // 1. NPM Audit
  try {
    console.log('📦 Running npm audit...');
    execSync('npm audit --audit-level moderate', { stdio: 'inherit' });
    results.npmAudit = true;
    console.log('✅ NPM audit passed\n');
  } catch (error) {
    console.log('❌ NPM audit found vulnerabilities\n');
  }
  
  // 2. Retire.js scan
  try {
    console.log('🔍 Running retire.js scan...');
    execSync('npx retire --path ./ --outputformat json --outputpath ./reports/retire-report.json', { stdio: 'inherit' });
    results.retireJs = true;
    console.log('✅ Retire.js scan passed\n');
  } catch (error) {
    console.log('❌ Retire.js found vulnerable dependencies\n');
  }
  
  // 3. Dependency version check
  try {
    console.log('📋 Checking dependency versions...');
    const packageJson = JSON.parse(fs.readFileSync('package.json', 'utf8'));
    const outdatedDeps = [];
    
    // Check for known vulnerable versions
    const vulnerablePackages = {
      'angular': '<15.0.0',
      'rxjs': '<7.0.0',
      'typescript': '<4.0.0'
    };
    
    Object.keys(vulnerablePackages).forEach(pkg => {
      const deps = { ...packageJson.dependencies, ...packageJson.devDependencies };
      if (deps[pkg] && deps[pkg].includes(vulnerablePackages[pkg].replace('<', ''))) {
        outdatedDeps.push(`${pkg}: ${deps[pkg]} (vulnerable)`);
      }
    });
    
    if (outdatedDeps.length === 0) {
      results.dependencyCheck = true;
      console.log('✅ All dependencies are up to date\n');
    } else {
      console.log('❌ Found outdated dependencies:');
      outdatedDeps.forEach(dep => console.log(`  - ${dep}`));
      console.log('');
    }
  } catch (error) {
    console.log('❌ Failed to check dependency versions\n');
  }
  
  // 4. Code analysis for security issues
  try {
    console.log('🔎 Analyzing code for security issues...');
    const securityIssues = [];
    
    // Check for dangerous patterns
    const dangerousPatterns = [
      { pattern: /innerHTML\s*=/, message: 'Potential XSS: innerHTML usage detected' },
      { pattern: /eval\s*\(/, message: 'Dangerous: eval() usage detected' },
      { pattern: /document\.write/, message: 'Potential XSS: document.write usage detected' },
      { pattern: /window\.open\s*\([^)]*['"]\s*javascript:/, message: 'Potential XSS: javascript: URL in window.open' }
    ];
    
    function scanDirectory(dir) {
      const files = fs.readdirSync(dir);
      
      files.forEach(file => {
        const filePath = path.join(dir, file);
        const stat = fs.statSync(filePath);
        
        if (stat.isDirectory() && !file.startsWith('.') && file !== 'node_modules') {
          scanDirectory(filePath);
        } else if (file.endsWith('.ts') || file.endsWith('.js')) {
          const content = fs.readFileSync(filePath, 'utf8');
          
          dangerousPatterns.forEach(({ pattern, message }) => {
            if (pattern.test(content)) {
              securityIssues.push(`${filePath}: ${message}`);
            }
          });
        }
      });
    }
    
    scanDirectory('./src');
    
    if (securityIssues.length === 0) {
      results.codeAnalysis = true;
      console.log('✅ No security issues found in code\n');
    } else {
      console.log('❌ Found potential security issues:');
      securityIssues.forEach(issue => console.log(`  - ${issue}`));
      console.log('');
    }
  } catch (error) {
    console.log('❌ Failed to analyze code for security issues\n');
  }
  
  // Generate report
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
    path.join(reportDir, 'security-audit.json'),
    JSON.stringify(report, null, 2)
  );
  
  // Summary
  console.log('📊 Security Audit Summary');
  console.log(`✅ Passed: ${report.summary.passed}/${report.summary.total} checks`);
  
  if (report.summary.passed === report.summary.total) {
    console.log('🎉 All security checks passed!');
    process.exit(0);
  } else {
    console.log('🚨 Some security checks failed. Review the issues above.');
    process.exit(1);
  }
}

runSecurityAudit();