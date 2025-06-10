# Common UI Components Library

A comprehensive set of reusable UI components for the Accessibility Reports application. All components follow consistent design patterns, accessibility standards, and include proper TypeScript support.

## 🎨 Components Overview

### Button Component
Flexible button component with multiple variants, sizes, and states.

**Usage:**
```typescript
import { ButtonComponent } from './components/common';

// Basic usage
<app-button>Click me</app-button>

// With variants and sizes
<app-button variant="primary" size="lg">Primary Button</app-button>
<app-button variant="success" size="sm" icon="check">Save</app-button>

// With loading state
<app-button [loading]="isSubmitting" (click)="onSubmit()">Submit</app-button>
```

**Props:**
- `variant`: 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'ghost'
- `size`: 'xs' | 'sm' | 'md' | 'lg' | 'xl'
- `disabled`: boolean
- `loading`: boolean
- `fullWidth`: boolean
- `type`: 'button' | 'submit' | 'reset'
- `icon`: string (icon name)
- `iconPosition`: 'left' | 'right'

**Events:**
- `(click)`: Emitted when button is clicked

---

### Modal Component
Flexible modal dialog with customizable sizes and content slots.

**Usage:**
```typescript
<app-modal 
  [isOpen]="showModal" 
  title="Add New Site" 
  size="lg"
  [showFooter]="true"
  (close)="closeModal()"
>
  <p>Modal content goes here</p>
  
  <div slot="footer">
    <app-button variant="secondary" (click)="closeModal()">Cancel</app-button>
    <app-button variant="primary" (click)="save()">Save</app-button>
  </div>
</app-modal>
```

**Props:**
- `isOpen`: boolean
- `title`: string
- `size`: 'sm' | 'md' | 'lg' | 'xl' | 'full'
- `showCloseButton`: boolean
- `showFooter`: boolean
- `closeOnOverlayClick`: boolean

**Events:**
- `(close)`: Emitted when modal should be closed

**Slots:**
- Default: Main modal content
- `[slot=footer]`: Footer content

---

### Input Component
Form input with labels, validation, and icon support.

**Usage:**
```typescript
<app-input 
  label="Website URL" 
  type="url" 
  placeholder="https://example.com"
  prefixIcon="globe"
  [required]="true"
  helpText="Enter the full URL including https://"
  (inputChange)="onUrlChange($event)"
></app-input>

// With error state
<app-input 
  label="Email" 
  type="email"
  [hasError]="true"
  errorMessage="Please enter a valid email address"
></app-input>
```

**Props:**
- `label`: string
- `placeholder`: string
- `type`: 'text' | 'email' | 'password' | 'number' | 'tel' | 'url' | 'search'
- `size`: 'sm' | 'md' | 'lg'
- `disabled`: boolean
- `readonly`: boolean
- `required`: boolean
- `helpText`: string
- `errorMessage`: string
- `hasError`: boolean
- `prefixIcon`: string
- `suffixIcon`: string

**Events:**
- `(inputChange)`: Emitted when input value changes
- `(blur)`: Emitted when input loses focus
- `(focus)`: Emitted when input gains focus

---

### Card Component
Flexible card container with header, body, and footer sections.

**Usage:**
```typescript
<app-card 
  title="Site Report" 
  subtitle="Last updated today"
  variant="primary"
  [hover]="true"
  [clickable]="true"
  (cardClick)="onCardClick()"
>
  <p>Card content goes here</p>
  
  <div slot="footer">
    <app-button size="sm">View Details</app-button>
  </div>
</app-card>
```

**Props:**
- `title`: string
- `subtitle`: string
- `variant`: 'default' | 'primary' | 'success' | 'warning' | 'error'
- `hover`: boolean
- `clickable`: boolean
- `elevated`: boolean
- `bordered`: boolean
- `noPadding`: boolean
- `showFooter`: boolean
- `headerAction`: boolean

**Events:**
- `(cardClick)`: Emitted when card is clicked (if clickable)

**Slots:**
- Default: Main card content
- `[slot=footer]`: Footer content
- `[slot=header-action]`: Header action area

---

### Badge Component
Small status indicators with multiple variants and styles.

**Usage:**
```typescript
<app-badge variant="success" icon="check">Fixed</app-badge>
<app-badge variant="error" [dismissible]="true" (dismiss)="onDismiss()">Error</app-badge>
<app-badge variant="warning" [outline]="true">Pending</app-badge>
<app-badge variant="info" [dot]="true"></app-badge>
```

**Props:**
- `variant`: 'primary' | 'secondary' | 'success' | 'warning' | 'error' | 'info'
- `size`: 'sm' | 'md' | 'lg'
- `outline`: boolean
- `dot`: boolean
- `dismissible`: boolean
- `icon`: string

**Events:**
- `(dismiss)`: Emitted when badge is dismissed

---

### Loading Component
Various loading indicators for different use cases.

**Usage:**
```typescript
<!-- Spinner -->
<app-loading type="spinner" text="Loading..." [center]="true"></app-loading>

<!-- Dots animation -->
<app-loading type="dots" size="lg"></app-loading>

<!-- Skeleton loader -->
<app-loading type="skeleton" [skeletonLines]="4"></app-loading>

<!-- Overlay loading -->
<app-loading type="spinner" [overlay]="true" text="Processing..."></app-loading>
```

**Props:**
- `type`: 'spinner' | 'dots' | 'pulse' | 'skeleton'
- `size`: 'sm' | 'md' | 'lg'
- `text`: string
- `center`: boolean
- `overlay`: boolean
- `skeletonLines`: number

---

### Alert Component
Contextual feedback messages with different severity levels.

**Usage:**
```typescript
<app-alert variant="success" title="Success!" [dismissible]="true">
  Your site has been successfully added.
</app-alert>

<app-alert variant="error" title="Error" [bordered]="true">
  Failed to scan the website. Please try again.
</app-alert>

<app-alert variant="warning" size="sm">
  This action cannot be undone.
</app-alert>
```

**Props:**
- `variant`: 'success' | 'warning' | 'error' | 'info'
- `size`: 'sm' | 'md' | 'lg'
- `title`: string
- `dismissible`: boolean
- `showIcon`: boolean
- `bordered`: boolean

**Events:**
- `(dismiss)`: Emitted when alert is dismissed

---

## 🎯 Design Principles

### Accessibility First
- All components include proper ARIA labels
- Keyboard navigation support
- Focus management
- Screen reader compatibility

### Consistent Styling
- Uses design system variables
- Consistent spacing and typography
- Hover states and transitions
- Mobile-responsive design

### TypeScript Support
- Full type safety
- Proper interfaces for all props
- Event type definitions
- Intellisense support

### Performance Optimized
- Standalone components
- OnPush change detection ready
- Minimal bundle impact
- Tree-shakeable exports

---

## 📦 Installation & Usage

### Import Components
```typescript
// Import individual components
import { ButtonComponent, ModalComponent } from './components/common';

// Or import all at once
import * as CommonComponents from './components/common';

@Component({
  imports: [ButtonComponent, ModalComponent, InputComponent]
})
```

### Global Styles
The components rely on CSS custom properties defined in `global_styles.css`. Ensure these are loaded in your application.

### Icons
Components use inline SVG icons for better performance and customization. Icons are defined within each component and can be extended as needed.

---

## 🔧 Customization

### Extending Variants
Add new variants by extending the component's SCSS files:

```scss
.btn-custom {
  background-color: var(--custom-color);
  color: white;
}
```

### Custom Icons
Add new icons to the component's icon map:

```typescript
const icons = {
  'custom-icon': '<path d="..."/>',
  // existing icons...
};
```

### Theme Variables
Customize the design system by modifying CSS custom properties in `global_styles.css`.

---

## 🚀 Best Practices

1. **Use semantic variants** - Choose variants that match the action's meaning
2. **Consistent sizing** - Stick to the predefined size scale
3. **Accessibility** - Always provide proper labels and ARIA attributes
4. **Performance** - Import only the components you need
5. **Testing** - Test components with keyboard navigation and screen readers

---

## 📋 Component Checklist

When creating new components, ensure they include:

- [ ] TypeScript interfaces for all props
- [ ] Proper accessibility attributes
- [ ] Responsive design
- [ ] Hover and focus states
- [ ] Loading/disabled states where applicable
- [ ] Consistent styling with design system
- [ ] Event emitters for user interactions
- [ ] Documentation and usage examples