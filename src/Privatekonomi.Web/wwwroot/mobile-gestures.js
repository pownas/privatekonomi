// Mobile Gesture Support for Privatekonomi
// Implements touch gestures for mobile-optimized UI

class MobileGestureHandler {
    constructor() {
        this.touchStartX = 0;
        this.touchStartY = 0;
        this.touchEndX = 0;
        this.touchEndY = 0;
        this.activeElement = null;
        this.swipeThreshold = 75; // Minimum distance for swipe
        this.pullThreshold = 100; // Minimum distance for pull-to-refresh
        this.isPulling = false;
        this.pullIndicator = null;
        this.dotNetHelper = null;
    }

    init(dotNetHelper) {
        this.dotNetHelper = dotNetHelper;
        this.setupPullToRefresh();
    }

    // Attach gesture listeners to the data grid via event delegation.
    // This replaces per-row registration and avoids one JS call per transaction.
    attachGesturesToGrid() {
        const grid = document.querySelector('.mud-table-container');
        if (!grid) return;

        // Avoid attaching duplicate listeners if called more than once
        if (grid._gesturesAttached) return;
        grid._gesturesAttached = true;

        grid.addEventListener('touchstart', (e) => {
            const row = e.target.closest('tr.mud-table-row');
            if (!row) return;
            const idAttr = row.getAttribute('data-transaction-id');
            const parsed = idAttr ? parseInt(idAttr, 10) : NaN;
            if (isNaN(parsed)) return;
            const itemId = parsed;
            this.handleTouchStart(e, itemId);
            this._activeRow = row;
        }, { passive: true });

        grid.addEventListener('touchmove', (e) => {
            if (!this._activeRow) return;
            this.handleTouchMove(e, this._activeRow);
        }, { passive: false });

        grid.addEventListener('touchend', (e) => {
            if (!this._activeRow) return;
            this.handleTouchEnd(e, this._activeRow, this.activeElement);
            this._activeRow = null;
        }, { passive: true });
    }

    // Attach swipe listeners to an element
    attachSwipeListeners(element, itemId) {
        if (!element) return;

        element.addEventListener('touchstart', (e) => {
            this.handleTouchStart(e, itemId);
        }, { passive: true });

        element.addEventListener('touchmove', (e) => {
            this.handleTouchMove(e, element);
        }, { passive: false });

        element.addEventListener('touchend', (e) => {
            this.handleTouchEnd(e, element, itemId);
        }, { passive: true });
    }

    handleTouchStart(e, itemId) {
        this.touchStartX = e.changedTouches[0].screenX;
        this.touchStartY = e.changedTouches[0].screenY;
        this.activeElement = itemId;
    }

    handleTouchMove(e, element) {
        this.touchEndX = e.changedTouches[0].screenX;
        this.touchEndY = e.changedTouches[0].screenY;

        const diffX = this.touchEndX - this.touchStartX;
        const diffY = Math.abs(this.touchEndY - this.touchStartY);

        // Only prevent default if horizontal swipe is dominant
        if (Math.abs(diffX) > diffY && Math.abs(diffX) > 10) {
            e.preventDefault();
            
            // Visual feedback for swipe
            if (diffX > 0) {
                // Swipe right - edit (green)
                element.style.transform = `translateX(${Math.min(diffX, 100)}px)`;
                element.style.backgroundColor = 'rgba(46, 125, 50, 0.1)';
            } else {
                // Swipe left - delete (red)
                element.style.transform = `translateX(${Math.max(diffX, -100)}px)`;
                element.style.backgroundColor = 'rgba(211, 47, 47, 0.1)';
            }
        }
    }

    handleTouchEnd(e, element, itemId) {
        const diffX = this.touchEndX - this.touchStartX;
        const diffY = Math.abs(this.touchEndY - this.touchStartY);

        // Reset visual state
        element.style.transform = '';
        element.style.backgroundColor = '';

        // Only trigger if horizontal swipe is dominant
        if (Math.abs(diffX) > diffY) {
            if (diffX > this.swipeThreshold) {
                // Swipe right - edit
                this.handleSwipe('right', itemId);
            } else if (diffX < -this.swipeThreshold) {
                // Swipe left - delete
                this.handleSwipe('left', itemId);
            }
        }

        // Reset
        this.touchStartX = 0;
        this.touchStartY = 0;
        this.touchEndX = 0;
        this.touchEndY = 0;
        this.activeElement = null;
    }

    handleSwipe(direction, itemId) {
        if (this.dotNetHelper) {
            this.dotNetHelper.invokeMethodAsync('HandleSwipeGesture', direction, itemId);
        }
    }

    // Pull-to-refresh functionality
    setupPullToRefresh() {
        let startY = 0;
        let currentY = 0;
        let pulling = false;

        document.addEventListener('touchstart', (e) => {
            if (window.scrollY === 0) {
                startY = e.touches[0].clientY;
                pulling = true;
            }
        }, { passive: true });

        document.addEventListener('touchmove', (e) => {
            if (!pulling) return;

            currentY = e.touches[0].clientY;
            const pullDistance = currentY - startY;

            if (pullDistance > 0 && pullDistance < 150) {
                e.preventDefault();
                this.updatePullIndicator(pullDistance);
            }
        }, { passive: false });

        document.addEventListener('touchend', (e) => {
            if (!pulling) return;

            const pullDistance = currentY - startY;

            if (pullDistance > this.pullThreshold) {
                this.triggerRefresh();
            }

            this.hidePullIndicator();
            pulling = false;
            startY = 0;
            currentY = 0;
        }, { passive: true });
    }

    updatePullIndicator(distance) {
        if (!this.pullIndicator) {
            this.pullIndicator = document.createElement('div');
            this.pullIndicator.id = 'pull-indicator';
            this.pullIndicator.innerHTML = `
                <div class="pull-indicator-content">
                    <svg class="pull-indicator-icon" viewBox="0 0 24 24">
                        <path d="M12 4V1L8 5l4 4V6c3.31 0 6 2.69 6 6 0 1.01-.25 1.97-.7 2.8l1.46 1.46C19.54 15.03 20 13.57 20 12c0-4.42-3.58-8-8-8zm0 14c-3.31 0-6-2.69-6-6 0-1.01.25-1.97.7-2.8L5.24 7.74C4.46 8.97 4 10.43 4 12c0 4.42 3.58 8 8 8v3l4-4-4-4v3z"/>
                    </svg>
                    <span class="pull-indicator-text">Dra ner för att uppdatera...</span>
                </div>
            `;
            document.body.appendChild(this.pullIndicator);
        }

        this.pullIndicator.style.display = 'block';
        this.pullIndicator.style.height = `${distance}px`;
        
        if (distance > this.pullThreshold) {
            this.pullIndicator.querySelector('.pull-indicator-text').textContent = 'Släpp för att uppdatera!';
        } else {
            this.pullIndicator.querySelector('.pull-indicator-text').textContent = 'Dra ner för att uppdatera...';
        }
    }

    hidePullIndicator() {
        if (this.pullIndicator) {
            this.pullIndicator.style.display = 'none';
        }
    }

    triggerRefresh() {
        if (this.pullIndicator) {
            this.pullIndicator.querySelector('.pull-indicator-text').textContent = 'Uppdaterar...';
            const icon = this.pullIndicator.querySelector('.pull-indicator-icon');
            icon.style.animation = 'spin 1s linear infinite';
        }

        if (this.dotNetHelper) {
            this.dotNetHelper.invokeMethodAsync('HandlePullToRefresh')
                .then(() => {
                    setTimeout(() => this.hidePullIndicator(), 500);
                });
        } else {
            // Fallback: reload page
            setTimeout(() => {
                location.reload();
            }, 500);
        }
    }

    // Check if device is mobile
    isMobileDevice() {
        return /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent) ||
               (window.matchMedia && window.matchMedia('(max-width: 768px)').matches);
    }

    // Enable/disable gestures based on viewport
    updateGestureState() {
        const isMobile = this.isMobileDevice();
        document.body.classList.toggle('mobile-gestures-enabled', isMobile);
        return isMobile;
    }

    dispose() {
        this.dotNetHelper = null;
        if (this.pullIndicator) {
            this.pullIndicator.remove();
            this.pullIndicator = null;
        }
    }
}

// Global instance
window.mobileGestureHandler = new MobileGestureHandler();

// Initialize gesture state on load
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        window.mobileGestureHandler.updateGestureState();
    });
} else {
    window.mobileGestureHandler.updateGestureState();
}

// Update on resize
window.addEventListener('resize', () => {
    window.mobileGestureHandler.updateGestureState();
});
