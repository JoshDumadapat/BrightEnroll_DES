// File download utility for Blazor
window.downloadFile = function (filename, contentType, content) {
    try {
        let blob;
        
        // Check if content is base64 encoded (for PDFs, Excel files, etc.)
        if ((contentType === 'application/pdf' || 
             contentType === 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' ||
             contentType === 'application/vnd.ms-excel') && 
            typeof content === 'string') {
            // Decode base64 string to binary
            const binaryString = atob(content);
            const bytes = new Uint8Array(binaryString.length);
            for (let i = 0; i < binaryString.length; i++) {
                bytes[i] = binaryString.charCodeAt(i);
            }
            blob = new Blob([bytes], { type: contentType });
        } else {
            // For text-based content (CSV, etc.)
            blob = new Blob([content], { type: contentType });
        }
        
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
    } catch (error) {
        console.error('Error downloading file:', error);
        alert('Error downloading file. Please try again.');
    }
};

