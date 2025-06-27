# Enhanced Scan Notification System

## Overview 🎯

The background scan worker now supports **different notification types** based on the scan tier, specifically designed to handle the requirements for **free scans** vs **premium scans**.

## Notification Types 📧

### 1. **RichWithPdf** (Default for Free Scans)
- **Full PDF report** generated and emailed
- **Accessibility score** included
- **Teaser image** (if available)  
- **Professional email template** with download links
- Uses the existing `SendScanResultEmailAsync` method

### 2. **Basic** (Default for Premium/Enterprise)
- **Simple completion notification**
- Just tells user "scan is done, check dashboard"
- No PDF attached (users access via web dashboard)

### 3. **None**
- **No email notification** sent
- For silent/internal scans

## How It Works 🔄

### 1. **API Request Configuration**

```typescript
// Free scan - gets rich email with PDF
POST /api/scans/start
{
  "siteUrl": "https://example.com",
  "scanTier": "free",                    // Optional - defaults to "free"
  "notificationType": "RichWithPdf"      // Optional - auto-determined from tier
}

// Premium scan - gets basic notification  
POST /api/scans/start
{
  "siteUrl": "https://example.com", 
  "scanTier": "premium",
  "notificationType": "Basic"
}
```

### 2. **Automatic Tier-Based Defaults**

The system automatically determines notification type based on `scanTier`:

```csharp
private static ScanNotificationType DetermineScanNotificationType(string? scanTier)
{
    return scanTier?.ToLowerInvariant() switch
    {
        "free" => ScanNotificationType.RichWithPdf,     // Free scans get rich emails
        "premium" => ScanNotificationType.Basic,        // Premium users use dashboard
        "enterprise" => ScanNotificationType.Basic,     // Enterprise users use dashboard
        null => ScanNotificationType.RichWithPdf,       // Default to free tier behavior
        _ => ScanNotificationType.Basic
    };
}
```

### 3. **Background Processing**

The `ScanWorkerService` handles different notification types:

```csharp
switch (job.NotificationType)
{
    case ScanNotificationType.RichWithPdf:
        // Generate PDF, upload to storage, send rich email
        await SendRichScanNotificationAsync(job, report, scanResult, services);
        break;
        
    case ScanNotificationType.Basic:
        // Send simple "scan completed" email
        await notificationService.NotifyScanCompletedAsync(userEmail, siteName, reportId);
        break;
        
    case ScanNotificationType.None:
        // No notification
        break;
}
```

## Rich Notification Process 📄

For **free scans** (`RichWithPdf`), the worker:

1. **Generates PDF Report**
   - Converts scan results to `AccessibilityReport` model
   - Renders HTML using `IReportBuilder`
   - Generates PDF from HTML

2. **Uploads to Storage**
   - Stores PDF with key: `reports/{reportId}/report.pdf`
   - Creates presigned URL (30-day expiry)

3. **Sends Rich Email**
   - Uses existing `SendScanResultEmailAsync` method
   - Includes PDF download link
   - Shows accessibility score
   - Includes teaser image (if available)

## Email Templates 📋

### Rich Email (Free Scans)
```html
<h2>Your Access Lens WCAG Snapshot is Ready</h2>
<p>Your WCAG Snapshot score is <strong>85/100</strong>.</p>
<img src="teaser-url" alt="Teaser" style="max-width:600px;"/>
<a href="pdf-download-url">Download PDF Report</a>
<p>Thanks for using Access Lens!</p>
```

### Basic Email (Premium Scans)  
```html
<h2>Scan Completed Successfully!</h2>
<p>Your accessibility scan for <strong>example.com</strong> has been completed.</p>
<p>You can view the results at: <a href='/reports/123'>View Report</a></p>
```

## Integration Examples 🔧

### Frontend Integration

```typescript
// For free tier users
const startFreeScan = async (url: string) => {
  const response = await fetch('/api/scans/start', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      siteUrl: url,
      scanTier: 'free',              // Will get RichWithPdf notification
      maxPages: 5                    // Free tier limit
    })
  });
  
  const { jobId } = await response.json();
  
  // Show message: "We'll email you the PDF report when ready!"
  showMessage(`Scan started! We'll email your PDF report to ${userEmail} when ready.`);
};

// For premium users
const startPremiumScan = async (url: string) => {
  const response = await fetch('/api/scans/start', {
    method: 'POST', 
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      siteUrl: url,
      scanTier: 'premium',           // Will get Basic notification
      maxPages: 100                  // Premium tier limit
    })
  });
  
  const { jobId } = await response.json();
  
  // Redirect to dashboard to watch progress
  window.location.href = `/dashboard/scans/${jobId}`;
};
```

### Controller Consolidation

**✅ COMPLETED**: All scan functionality has been consolidated into `ScansController`. The notification system now works seamlessly with:

- **`/api/scans/starter`**: Marketing scans with rich notifications (PDF + teaser)
- **`/api/scans/start`**: Authenticated scans with appropriate notification levels

All notification types are now properly configured automatically based on the scan context.

## Configuration ⚙️

### Service Registration
The notification services are already registered in DI:

```csharp
// Use email service for production
services.AddScoped<INotificationService, EmailNotificationService>();

// Or use console service for development  
services.AddScoped<INotificationService, ConsoleNotificationService>();
```

### Storage Configuration
PDF storage uses the existing `IStorageService`:

```json
{
  "S3": {
    "BucketName": "accesslens-reports",
    "Region": "us-east-1"
  }
}
```

## Benefits ✅

1. **Free Users Get Value**: Rich email with PDF keeps free users engaged
2. **Premium Users Avoid Spam**: Simple notifications, use dashboard for details  
3. **Flexible System**: Can override notification type per request
4. **Backward Compatible**: Legacy endpoints work with new system
5. **Storage Efficient**: PDFs only generated when needed
6. **Consistent Experience**: Same email templates as old synchronous system

## Next Steps 🚀

1. **Test Free Scan Flow**:
   ```bash
   POST /api/scans/start
   {
     "siteUrl": "https://example.com",
     "scanTier": "free"
   }
   ```

2. **Update Frontend**: Differentiate free vs premium scan flows

3. **Monitor Email Delivery**: Check logs for PDF generation and email sending

4. **Scale Storage**: Consider CDN for PDF downloads if volume increases

Your scan notification system now **perfectly handles both free and premium scan scenarios** with appropriate email experiences for each user type! 🎉
