// File download utility for Blazor
window.downloadFile = function (filename, contentType, content) {
    // Create a blob with the content
    const blob = new Blob([content], { type: contentType });
    const url = window.URL.createObjectURL(blob);
    
    // Create a temporary anchor element and trigger download
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    
    // Clean up
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
};

