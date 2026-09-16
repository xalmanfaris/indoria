// Indoria - Client-Side Interactive Behaviors & Theme System
document.addEventListener('DOMContentLoaded', () => {
    initTheme();
    initHomeProductTabs();
    initWishlist();
    initCart();
    initPincodeChecker();
    initProductGallery();
    initVariantSelectors();
    initDealCountdown();
    initCheckoutSteps();
    initCouponDemo();
    initQuickView();
    initQuickSearch();
});

function initQuickSearch() {
    const searchInput = document.getElementById('topNavSearchInput');
    const kbd = document.querySelector('.aura-search-kbd');
    if (!searchInput) return;

    const isMac = navigator.platform.toUpperCase().indexOf('MAC') >= 0;
    if (kbd) {
        kbd.textContent = isMac ? '⌘K' : 'Ctrl K';
    }

    document.addEventListener('keydown', (e) => {
        if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
            e.preventDefault();
            searchInput.focus();
            searchInput.select();
        }
    });
}

// Interactive Flagship Tabs on Home Screen
function initHomeProductTabs() {
    const filterContainer = document.getElementById('homeProductFilters');
    const productGrid = document.getElementById('homeProductGrid');
    if (!filterContainer || !productGrid) return;

    const filterBtns = filterContainer.querySelectorAll('.aura-pill-btn');
    const items = productGrid.querySelectorAll('.home-product-item');

    filterBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            const filter = btn.getAttribute('data-filter');

            // Update active pill state
            filterBtns.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');

            // Filter items with smooth transition
            items.forEach(item => {
                const itemCat = item.getAttribute('data-category');
                if (filter === 'all' || itemCat === filter) {
                    item.style.display = '';
                    item.style.opacity = '0';
                    item.style.transform = 'translateY(8px)';
                    setTimeout(() => {
                        item.style.transition = 'opacity 0.25s ease, transform 0.25s ease';
                        item.style.opacity = '1';
                        item.style.transform = 'translateY(0)';
                    }, 20);
                } else {
                    item.style.display = 'none';
                }
            });
        });
    });
}

function copyCouponCode(code) {
    if (navigator.clipboard) {
        navigator.clipboard.writeText(code).then(() => {
            if (typeof showAuraToast === 'function') {
                showAuraToast(`Coupon code <strong>${code}</strong> copied to clipboard!`, 'success');
            }
        }).catch(() => {
            if (typeof showAuraToast === 'function') {
                showAuraToast(`Coupon code: <strong>${code}</strong>`, 'dark');
            }
        });
    } else if (typeof showAuraToast === 'function') {
        showAuraToast(`Coupon code: <strong>${code}</strong>`, 'dark');
    }
}

// Theme Switching Engine (Simplified: Light & Midnight Dark only)
function initTheme() {
    const saved = localStorage.getItem('indoria-theme') === 'dark' ? 'dark' : 'light';
    applyTheme(saved, false);
}

function setAppTheme(theme) {
    const nextTheme = theme === 'dark' ? 'dark' : 'light';
    localStorage.setItem('indoria-theme', nextTheme);
    applyTheme(nextTheme, true);
}

function toggleAppTheme() {
    const current = localStorage.getItem('indoria-theme') === 'dark' ? 'dark' : 'light';
    setAppTheme(current === 'dark' ? 'light' : 'dark');
}

function applyTheme(theme, showNotice = false) {
    const effective = theme === 'dark' ? 'dark' : 'light';

    document.documentElement.setAttribute('data-theme', effective);
    document.documentElement.setAttribute('data-bs-theme', effective);

    updateThemeUI(effective);

    if (showNotice && typeof showAuraToast === 'function') {
        const label = effective === 'dark' ? 'Midnight Dark' : 'Light Luxe';
        showAuraToast(`Theme switched to <strong>${label}</strong>`, 'dark');
    }
}

function updateThemeUI(effectiveTheme) {
    const iconEl = document.getElementById('themeActiveIcon');
    const labelEl = document.getElementById('themeActiveLabel');

    if (iconEl) {
        if (effectiveTheme === 'dark') {
            iconEl.className = 'bi bi-moon-stars-fill';
        } else {
            iconEl.className = 'bi bi-sun-fill';
        }
        iconEl.style.color = '#000000';
    }

    if (labelEl) {
        labelEl.textContent = effectiveTheme === 'dark' ? 'Dark' : 'Light';
    }

    document.querySelectorAll('.theme-option').forEach(btn => {
        const val = btn.getAttribute('data-theme-value');
        const check = btn.querySelector('.theme-check');
        if (val === effectiveTheme) {
            btn.classList.add('active', 'fw-bold');
            if (check) check.classList.remove('d-none');
        } else {
            btn.classList.remove('active', 'fw-bold');
            if (check) check.classList.add('d-none');
        }
    });
}

// Toast notification helper
function showAuraToast(message, type = 'primary') {
    const container = document.getElementById('auraToastContainer');
    if (!container) return;

    const toastEl = document.createElement('div');
    toastEl.className = `toast align-items-center text-white bg-${type === 'success' ? 'success' : type === 'danger' ? 'danger' : 'dark'} border-0 show shadow-lg mb-2`;
    toastEl.setAttribute('role', 'alert');
    toastEl.setAttribute('aria-live', 'assertive');
    toastEl.setAttribute('aria-atomic', 'true');

    toastEl.innerHTML = `
        <div class="d-flex">
            <div class="toast-body d-flex align-items-center gap-2 py-3 px-3">
                <i class="bi ${type === 'success' ? 'bi-check-circle-fill text-success' : 'bi-info-circle-fill'} fs-5"></i>
                <div>${message}</div>
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
    `;

    container.appendChild(toastEl);

    setTimeout(() => {
        toastEl.classList.remove('show');
        setTimeout(() => toastEl.remove(), 300);
    }, 3500);
}

// Wishlist interaction
function initWishlist() {
    let wishlistCount = 4;
    const wishlistBadges = document.querySelectorAll('.aura-wishlist-count');

    document.querySelectorAll('.aura-wishlist-btn').forEach(btn => {
        btn.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();

            const isSaved = btn.classList.toggle('active');
            const icon = btn.querySelector('i');
            const productName = btn.getAttribute('data-product-name') || 'Item';

            if (isSaved) {
                wishlistCount++;
                if (icon) icon.className = 'bi bi-heart-fill text-danger';
                showAuraToast(`Added <strong>${productName}</strong> to your Wishlist`, 'success');
            } else {
                wishlistCount = Math.max(0, wishlistCount - 1);
                if (icon) icon.className = 'bi bi-heart';
                showAuraToast(`Removed <strong>${productName}</strong> from your Wishlist`, 'dark');
            }

            wishlistBadges.forEach(badge => badge.textContent = wishlistCount);
        });
    });
}

// Cart interactions & Drawer
function initCart() {
    let cartCount = 3;
    const cartBadges = document.querySelectorAll('.aura-cart-count');

    document.querySelectorAll('.aura-btn-cart').forEach(btn => {
        btn.addEventListener('click', (e) => {
            e.preventDefault();
            const productName = btn.getAttribute('data-product-name') || 'Appliance';
            cartCount++;
            cartBadges.forEach(badge => badge.textContent = cartCount);

            // Button feedback animation
            const originalHtml = btn.innerHTML;
            btn.innerHTML = '<i class="bi bi-check2"></i> Added!';
            btn.classList.add('btn-success');
            setTimeout(() => {
                btn.innerHTML = originalHtml;
                btn.classList.remove('btn-success');
            }, 1500);

            showAuraToast(`Added <strong>${productName}</strong> to your cart!`, 'success');
        });
    });

    // Quantity steppers in cart
    document.querySelectorAll('.aura-qty-stepper').forEach(stepper => {
        const minus = stepper.querySelector('.aura-qty-minus');
        const plus = stepper.querySelector('.aura-qty-plus');
        const input = stepper.querySelector('.aura-qty-input');

        if (minus && plus && input) {
            minus.addEventListener('click', () => {
                let val = parseInt(input.value) || 1;
                if (val > 1) {
                    input.value = val - 1;
                    updateCartTotals();
                }
            });
            plus.addEventListener('click', () => {
                let val = parseInt(input.value) || 1;
                input.value = val + 1;
                updateCartTotals();
            });
        }
    });

    // Remove item in cart demo
    document.querySelectorAll('.aura-cart-remove-btn').forEach(btn => {
        btn.addEventListener('click', (e) => {
            e.preventDefault();
            const row = btn.closest('.aura-cart-item-row');
            if (row) {
                row.style.opacity = '0';
                row.style.transform = 'translateX(20px)';
                row.style.transition = 'all 0.3s ease';
                setTimeout(() => {
                    row.remove();
                    cartCount = Math.max(0, cartCount - 1);
                    cartBadges.forEach(badge => badge.textContent = cartCount);
                    updateCartTotals();
                    showAuraToast('Item removed from cart', 'dark');
                }, 300);
            }
        });
    });
}

function updateCartTotals() {
    let subtotal = 0;
    document.querySelectorAll('.aura-cart-item-row').forEach(row => {
        const price = parseFloat(row.getAttribute('data-price') || '0');
        const qtyInput = row.querySelector('.aura-qty-input');
        const qty = qtyInput ? parseInt(qtyInput.value) || 1 : 1;
        const totalEl = row.querySelector('.aura-cart-item-total');
        const itemTotal = price * qty;
        if (totalEl) {
            totalEl.textContent = '₹' + itemTotal.toLocaleString('en-IN');
        }
        subtotal += itemTotal;
    });

    const subtotalEl = document.getElementById('cartSubtotal');
    const grandTotalEl = document.getElementById('cartGrandTotal');
    const discountEl = document.getElementById('cartDiscount');
    const discount = discountEl ? parseFloat(discountEl.getAttribute('data-discount') || '0') : 0;

    if (subtotalEl) subtotalEl.textContent = '₹' + subtotal.toLocaleString('en-IN');
    if (grandTotalEl) {
        const grand = Math.max(0, subtotal - discount);
        grandTotalEl.textContent = '₹' + grand.toLocaleString('en-IN');
    }
}

// Pincode availability check demo
function initPincodeChecker() {
    const btn = document.getElementById('checkPincodeBtn');
    const input = document.getElementById('pincodeInput');
    const resultBox = document.getElementById('pincodeResult');

    if (btn && input && resultBox) {
        btn.addEventListener('click', () => {
            const val = input.value.trim();
            if (val.length === 6 && /^\d+$/.test(val)) {
                btn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Checking...';
                setTimeout(() => {
                    btn.innerHTML = 'Check';
                    resultBox.className = 'mt-2 alert alert-success py-2 px-3 small d-flex align-items-center gap-2 mb-0';
                    resultBox.innerHTML = `
                        <i class="bi bi-geo-alt-fill text-success fs-6"></i>
                        <div>
                            <strong>Delivery available to ${val}</strong><br/>
                            <span class="text-muted">Estimated: Tomorrow by 2:00 PM • Free White-Glove Installation Included</span>
                        </div>
                    `;
                    resultBox.classList.remove('d-none');
                }, 600);
            } else {
                resultBox.className = 'mt-2 alert alert-danger py-2 px-3 small d-flex align-items-center gap-2 mb-0';
                resultBox.innerHTML = '<i class="bi bi-exclamation-circle-fill"></i> Please enter a valid 6-digit Indian PIN code';
                resultBox.classList.remove('d-none');
            }
        });
    }
}

// Product detail gallery thumbnail switcher
function initProductGallery() {
    const mainImg = document.getElementById('pdpMainImage');
    const thumbs = document.querySelectorAll('.aura-pdp-thumb');

    if (mainImg && thumbs.length) {
        thumbs.forEach(thumb => {
            thumb.addEventListener('click', () => {
                thumbs.forEach(t => t.classList.remove('active'));
                thumb.classList.add('active');
                const src = thumb.getAttribute('data-src');
                if (src) {
                    mainImg.style.opacity = '0.3';
                    setTimeout(() => {
                        mainImg.src = src;
                        mainImg.style.opacity = '1';
                    }, 150);
                }
            });
        });
    }
}

// Product variant pills
function initVariantSelectors() {
    document.querySelectorAll('.aura-variant-group').forEach(group => {
        const pills = group.querySelectorAll('.aura-variant-pill');
        pills.forEach(pill => {
            pill.addEventListener('click', () => {
                pills.forEach(p => p.classList.remove('active'));
                pill.classList.add('active');
            });
        });
    });
}

// Flash deal live countdown
function initDealCountdown() {
    const hoursEl = document.getElementById('dealHours');
    const minsEl = document.getElementById('dealMins');
    const secsEl = document.getElementById('dealSecs');

    if (!hoursEl || !minsEl || !secsEl) return;

    let totalSeconds = 14 * 3600 + 42 * 60 + 19;

    setInterval(() => {
        if (totalSeconds <= 0) totalSeconds = 24 * 3600;
        totalSeconds--;

        const h = Math.floor(totalSeconds / 3600);
        const m = Math.floor((totalSeconds % 3600) / 60);
        const s = totalSeconds % 60;

        hoursEl.textContent = String(h).padStart(2, '0');
        minsEl.textContent = String(m).padStart(2, '0');
        secsEl.textContent = String(s).padStart(2, '0');
    }, 1000);
}

// Multi-step checkout transitions
function initCheckoutSteps() {
    const stepTabs = document.querySelectorAll('.aura-checkout-step-pane');
    const stepIndicators = document.querySelectorAll('.aura-step-item');

    window.goToCheckoutStep = function(stepNumber) {
        stepTabs.forEach(pane => {
            const pStep = parseInt(pane.getAttribute('data-step') || '1');
            pane.classList.toggle('d-none', pStep !== stepNumber);
        });

        stepIndicators.forEach(item => {
            const iStep = parseInt(item.getAttribute('data-step') || '1');
            item.classList.remove('active', 'completed');
            if (iStep === stepNumber) {
                item.classList.add('active');
            } else if (iStep < stepNumber) {
                item.classList.add('completed');
            }
        });

        window.scrollTo({ top: 120, behavior: 'smooth' });
    };
}

// Coupon code demo
function initCouponDemo() {
    const applyBtn = document.getElementById('applyCouponBtn');
    const input = document.getElementById('couponCodeInput');
    const msg = document.getElementById('couponMessage');
    const discountEl = document.getElementById('cartDiscount');

    if (applyBtn && input) {
        applyBtn.addEventListener('click', () => {
            const code = input.value.trim().toUpperCase();
            if (code === 'LUXE5000' || code === 'INDORIA10' || code === 'AURA10' || code === 'FESTIVE') {
                if (discountEl) {
                    discountEl.setAttribute('data-discount', '5000');
                    discountEl.textContent = '-₹5,000';
                }
                if (msg) {
                    msg.className = 'text-success small mt-1 d-block';
                    msg.innerHTML = `<i class="bi bi-check-circle-fill"></i> Coupon <strong>${code}</strong> applied! ₹5,000 saved.`;
                }
                updateCartTotals();
                showAuraToast(`Coupon <strong>${code}</strong> applied successfully!`, 'success');
            } else {
                if (msg) {
                    msg.className = 'text-danger small mt-1 d-block';
                    msg.innerHTML = '<i class="bi bi-exclamation-circle-fill"></i> Invalid coupon code. Try <strong>LUXE5000</strong>';
                }
            }
        });
    }
}

// Quick View Modal
function initQuickView() {
    const modalEl = document.getElementById('quickViewModal');
    if (!modalEl) return;

    document.querySelectorAll('.aura-btn-quickview').forEach(btn => {
        btn.addEventListener('click', (e) => {
            e.preventDefault();
            const name = btn.getAttribute('data-name');
            const brand = btn.getAttribute('data-brand');
            const price = btn.getAttribute('data-price');
            const origPrice = btn.getAttribute('data-orig-price');
            const image = btn.getAttribute('data-image');
            const desc = btn.getAttribute('data-desc');
            const slug = btn.getAttribute('data-slug');
            const rating = btn.getAttribute('data-rating');
            const reviews = btn.getAttribute('data-reviews');

            const titleEl = document.getElementById('qvTitle');
            const brandEl = document.getElementById('qvBrand');
            const priceEl = document.getElementById('qvPrice');
            const origPriceEl = document.getElementById('qvOrigPrice');
            const imageEl = document.getElementById('qvImage');
            const descEl = document.getElementById('qvDesc');
            const linkEl = document.getElementById('qvLink');
            const ratingEl = document.getElementById('qvRating');
            const reviewsEl = document.getElementById('qvReviews');

            if (titleEl) titleEl.textContent = name;
            if (brandEl) brandEl.textContent = brand;
            if (priceEl) priceEl.textContent = '₹' + parseInt(price).toLocaleString('en-IN');
            if (origPriceEl) origPriceEl.textContent = '₹' + parseInt(origPrice).toLocaleString('en-IN');
            if (imageEl) imageEl.src = image;
            if (descEl) descEl.textContent = desc;
            if (linkEl) linkEl.href = '/product/' + slug;
            if (ratingEl) ratingEl.textContent = rating;
            if (reviewsEl) reviewsEl.textContent = '(' + reviews + ' reviews)';

            const modal = new bootstrap.Modal(modalEl);
            modal.show();
        });
    });
}
