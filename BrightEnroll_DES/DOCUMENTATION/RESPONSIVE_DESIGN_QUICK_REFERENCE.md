# Responsive Design Quick Reference

## Quick Responsive Classes Cheat Sheet

### Grid Layouts

```razor
<!-- 1 column mobile, 2 tablet, 3 desktop -->
<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">

<!-- 1 column mobile, 2 tablet, 4 desktop -->
<div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">

<!-- Stats cards pattern -->
<div class="mb-6 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
```

### Buttons

```razor
<!-- Full width mobile, auto desktop -->
<button class="w-full sm:w-auto px-4 py-2 sm:px-6 sm:py-2.5">

<!-- Button group - stack mobile, inline desktop -->
<div class="flex flex-col gap-3 sm:flex-row sm:gap-4">
    <button>Button 1</button>
    <button>Button 2</button>
</div>
```

### Form Inputs

```razor
<!-- Responsive input -->
<input class="w-full px-3 py-2 text-sm sm:px-4 sm:py-2.5 sm:text-base" />

<!-- Responsive label -->
<label class="mb-1 text-xs sm:mb-2 sm:text-sm">Label</label>

<!-- Form grid -->
<div class="grid grid-cols-1 gap-3 sm:grid-cols-2 sm:gap-4">
```

### Tables

```razor
<!-- Scrollable table -->
<div class="overflow-x-auto">
    <table class="w-full">
        <thead>
            <tr>
                <th class="px-4 py-3 text-xs sm:px-6 sm:text-sm">
```

### Cards

```razor
<!-- Responsive card padding -->
<div class="rounded-xl bg-white p-4 shadow-sm sm:p-6">

<!-- Card grid -->
<div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
```

### Typography

```razor
<!-- Responsive heading -->
<h1 class="text-xl font-bold sm:text-2xl">

<!-- Responsive body text -->
<p class="text-xs sm:text-sm">

<!-- Responsive subtitle -->
<p class="mt-1 text-xs text-gray-600 sm:text-sm">
```

### Headers

```razor
<!-- Page header with actions -->
<div class="mb-6 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
    <div>
        <h1 class="text-xl font-bold sm:text-2xl">Title</h1>
        <p class="mt-1 text-xs sm:text-sm">Subtitle</p>
    </div>
    <div class="flex gap-3">
        <button>Action</button>
    </div>
</div>
```

### Spacing

```razor
<!-- Responsive margin -->
<div class="mb-3 sm:mb-4">

<!-- Responsive padding -->
<div class="px-3 py-2 sm:px-4 sm:py-2.5">

<!-- Responsive gap -->
<div class="flex gap-3 sm:gap-4">
```

## Common Patterns

### Dashboard Stats
```razor
<div class="mb-6 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
    <div class="rounded-xl border border-gray-100 bg-white p-4 sm:p-6">
        <p class="text-xs text-gray-600 sm:text-sm">Metric</p>
        <p class="text-2xl font-bold sm:text-3xl">Value</p>
    </div>
</div>
```

### Form Section
```razor
<div class="mb-6 overflow-hidden rounded-xl bg-white shadow sm:mb-8">
    <div class="px-4 py-4 sm:px-6 sm:py-6">
        <h2 class="mb-4 text-lg font-bold sm:mb-6 sm:text-xl">Section</h2>
        <div class="grid grid-cols-1 gap-3 sm:grid-cols-2 sm:gap-4">
            <!-- Form fields -->
        </div>
    </div>
</div>
```

### Action Buttons
```razor
<div class="mt-6 flex flex-col gap-4 sm:flex-row sm:justify-end">
    <button class="w-full rounded-full px-6 py-2.5 sm:w-auto">Cancel</button>
    <button class="w-full rounded-full px-6 py-2.5 sm:w-auto">Submit</button>
</div>
```

## Breakpoint Reference

| Prefix | Min Width | Typical Device |
|--------|-----------|----------------|
| (none) | 0px | Mobile portrait |
| `sm:` | 640px | Mobile landscape, small tablet |
| `md:` | 768px | Tablet portrait |
| `lg:` | 1024px | Tablet landscape, laptop |
| `xl:` | 1280px | Desktop |
| `2xl:` | 1536px | Large desktop |

## Testing Shortcuts

### Chrome DevTools
- F12 ? Toggle device toolbar (Ctrl+Shift+M)
- Test common devices
- Throttle network speed

### Common Test Resolutions
- Mobile: 375x667 (iPhone SE)
- Tablet: 768x1024 (iPad)
- Desktop: 1920x1080 (Full HD)

## Quick Fixes

### Text too small on mobile?
```razor
<!-- Before -->
<p class="text-sm">

<!-- After -->
<p class="text-xs sm:text-sm">
```

### Buttons too small on mobile?
```razor
<!-- Before -->
<button class="px-4 py-2">

<!-- After -->
<button class="px-4 py-2 sm:px-6 sm:py-2.5">
```

### Cards not stacking on mobile?
```razor
<!-- Before -->
<div class="grid grid-cols-3 gap-4">

<!-- After -->
<div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
```

### Table overflowing?
```razor
<!-- Before -->
<table class="w-full">

<!-- After -->
<div class="overflow-x-auto">
    <table class="w-full">
```

## Remember
- ? Mobile-first approach
- ? Touch targets: minimum 44x44px
- ? Test on real devices
- ? Check horizontal scroll
- ? Verify text readability
