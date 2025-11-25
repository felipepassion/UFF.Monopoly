window.boardBuilder = window.boardBuilder || {};

// Repositions popup element so it stays fully inside viewport.
// Accepts element id string. The element must be position:fixed.
window.boardBuilder.repositionPopup = function(id){
    try {
        const el = document.getElementById(id);
        if(!el) return;
        const rect = el.getBoundingClientRect();
        const vw = window.innerWidth; const vh = window.innerHeight;
        let dx = 0, dy = 0;
        const margin = 8; // small breathing space from edges
        if(rect.right > vw - margin){ dx -= (rect.right - (vw - margin)); }
        if(rect.bottom > vh - margin){ dy -= (rect.bottom - (vh - margin)); }
        if(rect.left + dx < margin){ dx += (margin - (rect.left + dx)); }
        if(rect.top + dy < margin){ dy += (margin - (rect.top + dy)); }
        if(dx !== 0 || dy !== 0){
            // Apply translated position via CSS transform to avoid layout thrash.
            const prev = el.dataset.baseTransform || 'translate(0px,0px)';
            // We only want to offset once per open; store final offset.
            el.style.transform = `translate(${dx}px, ${dy}px)`;
        }
        // Add a class when clamped for potential styling (e.g., drop shadow variation)
        if(dx !== 0 || dy !== 0) el.classList.add('popup-clamped'); else el.classList.remove('popup-clamped');
    } catch(err){ /* swallow */ }
};

// Optional: re-apply on resize for active popup
window.addEventListener('resize', () => {
    document.querySelectorAll('.type-popup').forEach(el => {
        if(el.id) window.boardBuilder.repositionPopup(el.id);
    });
});
