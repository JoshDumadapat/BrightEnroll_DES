// File download utility for Blazor
window.downloadFile = function (filename, contentType, content) {
    try {
        // Validate inputs
        if (!filename || !contentType || !content) {
            console.error('Invalid parameters for downloadFile:', { filename, contentType, content: content ? 'present' : 'missing' });
            alert('Error: Invalid file parameters. Please try again.');
            return;
        }

        let blob;
        
        // Check if content is base64 encoded (for PDFs, Excel files, etc.)
        if ((contentType === 'application/pdf' || 
             contentType === 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' ||
             contentType === 'application/vnd.ms-excel') && 
            typeof content === 'string') {
            try {
                // Decode base64 string to binary
                const binaryString = atob(content);
                const bytes = new Uint8Array(binaryString.length);
                for (let i = 0; i < binaryString.length; i++) {
                    bytes[i] = binaryString.charCodeAt(i);
                }
                blob = new Blob([bytes], { type: contentType });
            } catch (base64Error) {
                console.error('Error decoding base64 content:', base64Error);
                alert('Error: Invalid file data. Please try again.');
                return;
            }
        } else {
            // For text-based content (CSV, etc.)
            blob = new Blob([content], { type: contentType });
        }
        
        if (!blob || blob.size === 0) {
            console.error('Error: Generated blob is empty');
            alert('Error: File data is empty. Please try again.');
            return;
        }
        
        const url = window.URL.createObjectURL(blob);
        
        // Create a temporary anchor element and trigger download
        const link = document.createElement('a');
        link.href = url;
        link.download = filename;
        link.style.display = 'none';
        document.body.appendChild(link);
        
        // Trigger download
        link.click();
        
        // Clean up after a short delay to ensure download starts
        setTimeout(() => {
            document.body.removeChild(link);
            window.URL.revokeObjectURL(url);
        }, 100);
        
        console.log('File download initiated:', filename);
    } catch (error) {
        console.error('Error downloading file:', error);
        alert('Error downloading file: ' + error.message + '. Please try again.');
    }
};

// Print report utility for Blazor
window.printReport = function (htmlContent) {
    try {
        if (!htmlContent) {
            console.error('Invalid HTML content for printReport');
            alert('Error: No content to print. Please try again.');
            return;
        }

        // Create a new window for printing
        const printWindow = window.open('', '_blank', 'width=800,height=600');
        
        if (!printWindow) {
            alert('Please allow pop-ups to print the report.');
            return;
        }

        // Write HTML content to the new window
        printWindow.document.write(htmlContent);
        printWindow.document.close();

        // Wait for content to load, then trigger print
        printWindow.onload = function() {
            setTimeout(function() {
                printWindow.print();
                // Optionally close the window after printing (user can cancel)
                // printWindow.close();
            }, 250);
        };
    } catch (error) {
        console.error('Error printing report:', error);
        alert('Error printing report: ' + error.message + '. Please try again.');
    }
};

