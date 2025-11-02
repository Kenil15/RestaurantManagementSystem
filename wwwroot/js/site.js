// Restaurant Ordering System JavaScript

document.addEventListener('DOMContentLoaded', function () {
    // Initialize tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Update cart count on page load
    updateCartCount();

    // Auto-dismiss alerts after 5 seconds
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        setTimeout(() => {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }, 5000);
    });

    // Form validation enhancements
    const forms = document.querySelectorAll('form');
    forms.forEach(form => {
        if (form.dataset.skipDisable === 'true') {
            return; // don't auto-disable submit on forms that opt-out
        }
        form.addEventListener('submit', function () {
            const submitButton = this.querySelector('button[type="submit"]');
            if (submitButton) {
                submitButton.disabled = true;
                submitButton.innerHTML = '<span class="loading"></span> Processing...';
            }
        });
    });

    // Quantity input controls
    document.querySelectorAll('.quantity-input').forEach(input => {
        input.addEventListener('change', function () {
            const min = parseInt(this.min) || 1;
            const max = parseInt(this.max) || 99;
            let value = parseInt(this.value) || min;

            if (value < min) value = min;
            if (value > max) value = max;

            this.value = value;
        });
    });

    // Price formatting
    document.querySelectorAll('.price').forEach(priceElement => {
        const price = parseFloat(priceElement.textContent.replace('$', ''));
        if (!isNaN(price)) {
            priceElement.textContent = '$' + price.toFixed(2);
        }
    });
});

// Update cart count from server
function updateCartCount() {
    fetch('/Cart/GetCartCount')
        .then(response => response.json())
        .then(data => {
            const cartCountElement = document.getElementById('cartCount');
            if (cartCountElement) {
                cartCountElement.textContent = data.count;
                if (data.count > 0) {
                    cartCountElement.style.display = 'block';
                } else {
                    cartCountElement.style.display = 'none';
                }
            }
        })
        .catch(error => console.error('Error updating cart count:', error));
}

// Add to cart with feedback
function addToCart(menuItemId, button) {
    const originalText = button.innerHTML;
    button.disabled = true;
    button.innerHTML = '<span class="loading"></span>';

    fetch('/Cart/AddToCart', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: `menuItemId=${menuItemId}&quantity=1`
    })
        .then(response => {
            if (response.ok) {
                updateCartCount();
                showToast('Item added to cart!', 'success');
            } else {
                showToast('Failed to add item to cart', 'error');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            showToast('An error occurred', 'error');
        })
        .finally(() => {
            button.disabled = false;
            button.innerHTML = originalText;
        });
}

// Show toast notification
function showToast(message, type = 'info') {
    const toastContainer = document.getElementById('toastContainer') || createToastContainer();

    const toastEl = document.createElement('div');
    toastEl.className = `toast align-items-center text-bg-${type === 'error' ? 'danger' : type} border-0`;
    toastEl.setAttribute('role', 'alert');
    toastEl.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">${message}</div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>
    `;

    toastContainer.appendChild(toastEl);
    const toast = new bootstrap.Toast(toastEl);
    toast.show();

    // Remove toast after it's hidden
    toastEl.addEventListener('hidden.bs.toast', () => {
        toastEl.remove();
    });
}

// Create toast container if it doesn't exist
function createToastContainer() {
    const container = document.createElement('div');
    container.id = 'toastContainer';
    container.className = 'toast-container position-fixed top-0 end-0 p-3';
    container.style.zIndex = '9999';
    document.body.appendChild(container);
    return container;
}

// Search functionality
function performSearch(searchTerm) {
    if (searchTerm.length >= 2) {
        // Implement search logic here
        console.log('Searching for:', searchTerm);
    }
}

// Image error handling
document.addEventListener('DOMContentLoaded', function () {
    const placeholderDataUri = 'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="400" height="300"><rect width="100%" height="100%" fill="%23f0f0f0"/><text x="50%" y="50%" dominant-baseline="middle" text-anchor="middle" fill="%23999" font-family="Arial, Helvetica, sans-serif" font-size="18">Image not available</text></svg>';
    document.querySelectorAll('img').forEach(img => {
        img.addEventListener('error', function onImgError() {
            if (this.dataset.placeholderApplied === 'true') {
                return; // prevent loops
            }
            this.dataset.placeholderApplied = 'true';
            this.loading = 'lazy';
            this.src = placeholderDataUri;
            this.alt = 'Image not available';
        }, { once: false });
    });
});

// Smooth scrolling for anchor links
document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
        e.preventDefault();
        const target = document.querySelector(this.getAttribute('href'));
        if (target) {
            target.scrollIntoView({
                behavior: 'smooth',
                block: 'start'
            });
        }
    });
});