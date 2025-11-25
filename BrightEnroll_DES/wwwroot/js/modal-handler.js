// Modal click outside handler - plays sound and shakes modal instead of closing
window.handleModalBackdropClick = function (modalElementId) {
    try {
        console.log('handleModalBackdropClick called with ID:', modalElementId);
        const modalElement = document.getElementById(modalElementId);
        if (!modalElement) {
            console.warn('Modal element not found:', modalElementId);
            // Still play sound even if modal not found
            playNotificationSound();
            return;
        }

        // Play Windows notification sound
        playNotificationSound();

        // Add shake animation
        modalElement.classList.add('modal-shake');
        setTimeout(() => {
            modalElement.classList.remove('modal-shake');
        }, 500);
    } catch (error) {
        console.error('Error in handleModalBackdropClick:', error);
        // Still try to play sound
        try {
            playNotificationSound();
        } catch (e) {
            console.error('Error playing sound:', e);
        }
    }
};

// Play Windows-like notification sound
function playNotificationSound() {
    try {
        // Create audio context for generating a beep sound
        const AudioContextClass = window.AudioContext || window.webkitAudioContext;
        if (!AudioContextClass) {
            console.warn('AudioContext not supported');
            return;
        }

        let audioContext = window.audioContextInstance;
        if (!audioContext) {
            audioContext = new AudioContextClass();
            window.audioContextInstance = audioContext;
        }

        // Resume audio context if suspended (required for some browsers)
        if (audioContext.state === 'suspended') {
            audioContext.resume().catch(err => {
                console.warn('Could not resume audio context:', err);
            });
        }

        const oscillator = audioContext.createOscillator();
        const gainNode = audioContext.createGain();

        oscillator.connect(gainNode);
        gainNode.connect(audioContext.destination);

        // Configure for Windows-like beep
        oscillator.frequency.value = 800; // Frequency in Hz
        oscillator.type = 'sine';

        gainNode.gain.setValueAtTime(0.3, audioContext.currentTime);
        gainNode.gain.exponentialRampToValueAtTime(0.01, audioContext.currentTime + 0.1);

        oscillator.start(audioContext.currentTime);
        oscillator.stop(audioContext.currentTime + 0.1);
    } catch (error) {
        console.error('Error playing notification sound:', error);
    }
}

