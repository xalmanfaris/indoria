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

function initTheme() {
    const saved = localStorage.getItem('indoria-theme') || 'light';
    applyTheme(saved);
}

function toggleAppTheme() {
    const current = document.documentElement.getAttribute('data-theme') || 'light';
    const next = current === 'dark' ? 'light' : 'dark';
    applyTheme(next);
}

function applyTheme(theme) {
    document.documentElement.setAttribute('data-theme', theme);
    document.documentElement.setAttribute('data-bs-theme', theme);
    try {
        localStorage.setItem('indoria-theme', theme);
    } catch (e) {}
}

function initQuickSearch() {
    const searchTrigger = document.getElementById('searchTriggerBtn');
    const searchWrapper = document.getElementById('auraSearchInlineExpand');
    const searchInput = document.getElementById('topNavSearchInput');
    const searchClose = document.getElementById('searchCloseBtn');
    const resultsDropdown = document.getElementById('auraSearchResultsDropdown');

    if (!searchWrapper || !searchInput) return;

    let debounceTimer = null;

    function openSearch() {
        searchWrapper.classList.add('active');
        setTimeout(() => {
            if (searchInput) {
                searchInput.focus();
            }
        }, 50);
    }

    function closeSearch() {
        searchWrapper.classList.remove('active');
        hideResults();
    }

    function hideResults() {
        if (resultsDropdown) {
            resultsDropdown.classList.remove('show');
            resultsDropdown.innerHTML = '';
        }
    }

    async function performLiveSearch(query) {
        if (!resultsDropdown) return;

        const trimmed = query.trim();
        if (trimmed.length < 1) {
            hideResults();
            return;
        }

        try {
            const response = await fetch(`/api/search/live?q=${encodeURIComponent(trimmed)}`);
            if (!response.ok) return;

            const items = await response.json();

            if (items.length === 0) {
                resultsDropdown.innerHTML = `
                    <div class="p-3 text-center text-muted small">
                        <i class="bi bi-search me-1"></i> No matching products found for "<strong>${escapeHtml(trimmed)}</strong>"
                    </div>`;
            } else {
                let html = items.map(item => `
                    <a href="/product/${item.slug}" class="aura-search-live-item">
                        <img src="${item.image}" alt="${escapeHtml(item.name)}" class="aura-search-live-thumb" referrerpolicy="no-referrer" onerror="this.onerror=null;this.src='/images/homixa_hero_appliances.jpg';" />
                        <div class="flex-grow-1 overflow-hidden">
                            <div class="aura-search-live-title text-truncate">${escapeHtml(item.name)}</div>
                            <div class="aura-search-live-meta">${escapeHtml(item.brand)} • ${escapeHtml(item.category)}</div>
                            <div class="aura-search-live-price">${item.price}</div>
                        </div>
                    </a>
                `).join('');

                html += `
                    <a href="/search?q=${encodeURIComponent(trimmed)}" class="d-block text-center p-2 small fw-bold text-primary bg-light border-top rounded-bottom-4 text-decoration-none">
                        See all results for "${escapeHtml(trimmed)}" <i class="bi bi-arrow-right ms-1"></i>
                    </a>`;

                resultsDropdown.innerHTML = html;
            }

            resultsDropdown.classList.add('show');
        } catch (err) {
            console.error("Live search failed", err);
        }
    }

    function escapeHtml(str) {
        return str.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
    }

    searchInput.addEventListener('input', (e) => {
        clearTimeout(debounceTimer);
        const val = e.target.value;
        debounceTimer = setTimeout(() => {
            performLiveSearch(val);
        }, 150);
    });

    if (searchTrigger) {
        searchTrigger.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            openSearch();
        });
    }

    if (searchClose) {
        searchClose.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            closeSearch();
        });
    }

    document.addEventListener('keydown', (e) => {
        if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
            e.preventDefault();
            if (searchWrapper.classList.contains('active')) {
                closeSearch();
            } else {
                openSearch();
            }
        } else if (e.key === 'Escape' && searchWrapper.classList.contains('active')) {
            closeSearch();
        }
    });

    document.addEventListener('click', (e) => {
        if (searchWrapper.classList.contains('active') && !searchWrapper.contains(e.target)) {
            closeSearch();
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
                const itemCat = item.getAttribute('data-category') || '';
                let matches = false;

                if (filter === 'all') {
                    matches = true;
                } else if (filter === 'kitchen-appliances') {
                    matches = (itemCat === 'kitchen-appliances' || itemCat === 'dishwashers' || itemCat === 'microwaves');
                } else {
                    matches = (itemCat === filter);
                }

                if (matches) {
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
            iconEl.style.color = '#fbbf24';
        } else {
            iconEl.className = 'bi bi-sun-fill';
            iconEl.style.color = '#f59e0b';
        }
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
async function initWishlist() {
    const wishlistBadges = document.querySelectorAll('.aura-wishlist-count');

    // On page load, sync wishlist state from DB if authenticated
    if (window.isUserAuthenticated) {
        try {
            const response = await fetch('/api/v1/wishlist/status');
            if (response.ok) {
                const data = await response.json();
                wishlistBadges.forEach(badge => badge.textContent = data.count || 0);

                if (data.productIds && Array.isArray(data.productIds)) {
                    data.productIds.forEach(id => {
                        document.querySelectorAll(`.aura-wishlist-btn[data-product-id="${id}"]`).forEach(btn => {
                            btn.classList.add('active');
                            const icon = btn.querySelector('i');
                            if (icon) icon.className = 'bi bi-heart-fill text-danger';
                        });
                    });
                }
            }
        } catch (err) {
            console.error("Failed to fetch wishlist status", err);
        }
    }

    document.querySelectorAll('.aura-wishlist-btn').forEach(btn => {
        btn.addEventListener('click', async (e) => {
            e.preventDefault();
            e.stopPropagation();

            if (!window.isUserAuthenticated) {
                window.location.href = '/login?returnUrl=' + encodeURIComponent(window.location.pathname);
                return;
            }

            const productId = btn.getAttribute('data-product-id');
            const productName = btn.getAttribute('data-product-name') || 'Appliance';

            if (!productId) return;

            try {
                const res = await fetch(`/api/v1/wishlist/toggle/${encodeURIComponent(productId)}`, {
                    method: 'POST'
                });

                if (res.status === 401) {
                    window.location.href = '/login?returnUrl=' + encodeURIComponent(window.location.pathname);
                    return;
                }

                if (res.ok) {
                    const data = await res.json();
                    if (data.success) {
                        // Toggle active icon on all matching product buttons on page
                        document.querySelectorAll(`.aura-wishlist-btn[data-product-id="${productId}"]`).forEach(b => {
                            const icon = b.querySelector('i');
                            if (data.isWishlisted) {
                                b.classList.add('active');
                                if (icon) icon.className = 'bi bi-heart-fill text-danger';
                            } else {
                                b.classList.remove('active');
                                if (icon) icon.className = 'bi bi-heart';
                            }
                        });

                        wishlistBadges.forEach(badge => badge.textContent = data.count);

                        if (data.isWishlisted) {
                            showAuraToast(`Added <strong>${productName}</strong> to your Wishlist`, 'success');
                        } else {
                            showAuraToast(`Removed <strong>${productName}</strong> from your Wishlist`, 'dark');
                        }
                    }
                }
            } catch (err) {
                console.error("Failed to toggle wishlist", err);
            }
        });
    });
}

// Cart interactions & Drawer
async function initCart() {
    const cartBadges = document.querySelectorAll('.aura-cart-badge, .aura-cart-count');

    // Sync cart badge count from DB if user is authenticated
    if (window.isUserAuthenticated) {
        try {
            const response = await fetch('/api/v1/cart/status');
            if (response.ok) {
                const data = await response.json();
                cartBadges.forEach(badge => badge.textContent = data.count || 0);
            }
        } catch (err) {
            console.error("Failed to fetch cart count", err);
        }
    }

    // Add to Cart Buttons
    document.querySelectorAll('.aura-btn-cart').forEach(btn => {
        btn.addEventListener('click', async (e) => {
            e.preventDefault();

            if (!window.isUserAuthenticated) {
                window.location.href = '/login?returnUrl=' + encodeURIComponent(window.location.pathname);
                return;
            }

            const productId = btn.getAttribute('data-product-id');
            const productName = btn.getAttribute('data-product-name') || 'Appliance';

            if (!productId) return;

            // Gather PDP quantity / variants if present
            const qtyInput = document.querySelector('.aura-qty-input');
            const quantity = qtyInput ? (parseInt(qtyInput.value) || 1) : 1;
            const activeCapacity = document.querySelector('.aura-variant-pill.active');
            const capacity = activeCapacity ? activeCapacity.textContent.trim() : '';

            try {
                const res = await fetch('/api/v1/cart/add', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ productId, quantity, capacity, color: '' })
                });

                if (res.status === 401) {
                    window.location.href = '/login?returnUrl=' + encodeURIComponent(window.location.pathname);
                    return;
                }

                if (res.ok) {
                    const data = await res.json();
                    if (data.success) {
                        cartBadges.forEach(badge => badge.textContent = data.count);

                        // Button feedback animation
                        const originalHtml = btn.innerHTML;
                        btn.innerHTML = '<i class="bi bi-check2"></i> Added!';
                        btn.classList.add('btn-success');
                        setTimeout(() => {
                            btn.innerHTML = originalHtml;
                            btn.classList.remove('btn-success');
                        }, 1500);

                        showAuraToast(`Added <strong>${productName}</strong> to your cart!`, 'success');
                    }
                }
            } catch (err) {
                console.error("Failed to add item to cart", err);
            }
        });
    });

    // Buy Now Buttons
    document.querySelectorAll('.aura-btn-pdp-buy, .aura-btn-buy').forEach(btn => {
        btn.addEventListener('click', async (e) => {
            if (!window.isUserAuthenticated) {
                e.preventDefault();
                window.location.href = '/login?returnUrl=' + encodeURIComponent(window.location.pathname);
                return;
            }

            const productId = btn.closest('.col-lg-6')?.querySelector('.aura-btn-cart')?.getAttribute('data-product-id');
            if (productId) {
                e.preventDefault();
                const qtyInput = document.querySelector('.aura-qty-input');
                const quantity = qtyInput ? (parseInt(qtyInput.value) || 1) : 1;
                try {
                    await fetch('/api/v1/cart/add', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ productId, quantity, capacity: '', color: '' })
                    });
                } catch (err) {}
                window.location.href = '/checkout';
            }
        });
    });

    // Quantity Steppers in Cart Page
    document.querySelectorAll('.aura-cart-item-row .aura-qty-stepper').forEach(stepper => {
        const minus = stepper.querySelector('.aura-qty-minus');
        const plus = stepper.querySelector('.aura-qty-plus');
        const input = stepper.querySelector('.aura-qty-input');
        const row = stepper.closest('.aura-cart-item-row');
        const productId = row?.getAttribute('data-product-id');

        if (minus && plus && input && productId) {
            minus.addEventListener('click', async () => {
                let val = parseInt(input.value) || 1;
                if (val > 1) {
                    val--;
                    input.value = val;
                    await updateCartQuantityInDb(productId, val, row);
                }
            });
            plus.addEventListener('click', async () => {
                let val = parseInt(input.value) || 1;
                val++;
                input.value = val;
                await updateCartQuantityInDb(productId, val, row);
            });
        }
    });

    // Remove Item Buttons in Cart Page
    document.querySelectorAll('.aura-cart-remove-btn').forEach(btn => {
        btn.addEventListener('click', async (e) => {
            e.preventDefault();
            const row = btn.closest('.aura-cart-item-row');
            const productId = btn.getAttribute('data-product-id') || row?.getAttribute('data-product-id');

            if (row && productId) {
                try {
                    const res = await fetch(`/api/v1/cart/remove/${encodeURIComponent(productId)}`, {
                        method: 'POST'
                    });
                    if (res.ok) {
                        const data = await res.json();
                        row.style.opacity = '0';
                        row.style.transform = 'translateX(20px)';
                        row.style.transition = 'all 0.3s ease';
                        setTimeout(() => {
                            row.remove();
                            cartBadges.forEach(badge => badge.textContent = data.count);
                            updateCartTotals();
                            showAuraToast('Item removed from cart', 'dark');

                            if (data.count === 0) {
                                window.location.reload();
                            }
                        }, 300);
                    }
                } catch (err) {
                    console.error("Failed to remove item from cart", err);
                }
            }
        });
    });
}

async function updateCartQuantityInDb(productId, quantity, row) {
    const cartBadges = document.querySelectorAll('.aura-cart-badge, .aura-cart-count');
    try {
        const res = await fetch('/api/v1/cart/update', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ productId, quantity })
        });
        if (res.ok) {
            const data = await res.json();
            cartBadges.forEach(badge => badge.textContent = data.count);
            updateCartTotals();
        }
    } catch (err) {
        console.error("Failed to update cart quantity", err);
    }
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

    stepIndicators.forEach(item => {
        item.addEventListener('click', () => {
            const stepNum = parseInt(item.getAttribute('data-step') || '1');
            window.goToCheckoutStep(stepNum);
        });
    });

    window.goToCheckoutStep = function(stepNumber) {
        stepTabs.forEach(pane => {
            const pStep = parseInt(pane.getAttribute('data-step') || '1');
            pane.classList.toggle('d-none', pStep !== stepNumber);
        });

        stepIndicators.forEach(item => {
            const iStep = parseInt(item.getAttribute('data-step') || '1');
            item.classList.remove('active', 'completed');
            const icon = item.querySelector('.aura-step-circle-icon');
            if (iStep === stepNumber) {
                item.classList.add('active');
                if (icon) icon.innerHTML = iStep;
            } else if (iStep < stepNumber) {
                item.classList.add('completed');
                if (icon) icon.innerHTML = '<i class="bi bi-check-lg"></i>';
            } else {
                if (icon) icon.innerHTML = iStep;
            }
        });

        window.scrollTo({ top: 100, behavior: 'smooth' });
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
                    msg.className = 'text-success small mt-1 d-block fw-semibold';
                    msg.innerHTML = `<i class="bi bi-check-circle-fill me-1"></i> Coupon <strong>${code}</strong> applied! ₹5,000 saved.`;
                }
                updateCartTotals();
                if (typeof showAuraToast === 'function') {
                    showAuraToast(`Coupon <strong>${code}</strong> applied successfully!`, 'success');
                }
            } else {
                if (msg) {
                    msg.className = 'text-danger small mt-1 d-block fw-semibold';
                    msg.innerHTML = '<i class="bi bi-exclamation-circle-fill me-1"></i> Invalid coupon code. Try <strong>LUXE5000</strong>';
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

/* ==========================================================================
   Multi-Currency, Multi-Language, Multi-Country Preference Helpers
   ========================================================================== */
function changeAppCurrency(currencyCode) {
    fetch('/api/preferences/currency', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ value: currencyCode })
    }).then(res => res.json()).then(data => {
        if (data.success) {
            window.location.reload();
        }
    }).catch(err => console.error(err));
}

function changeAppLanguage(languageCode) {
    fetch('/api/preferences/language', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ value: languageCode })
    }).then(res => res.json()).then(data => {
        if (data.success) {
            window.location.reload();
        }
    }).catch(err => console.error(err));
}

function changeAppCountry(countryCode) {
    fetch('/api/preferences/country', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ value: countryCode })
    }).then(res => res.json()).then(data => {
        if (data.success) {
            window.location.reload();
        }
    }).catch(err => console.error(err));
}

/* ==========================================================================
   Customer Notifications Bell Panel
   ========================================================================== */
function initNotificationsBell() {
    fetchUserNotifications();
}

function fetchUserNotifications() {
    fetch('/api/notifications')
        .then(res => res.json())
        .then(data => {
            if (!data.success) return;

            const badge = document.getElementById('headerUnreadBadge');
            if (badge) {
                if (data.unreadCount > 0) {
                    badge.textContent = data.unreadCount;
                    badge.style.display = 'inline-block';
                } else {
                    badge.style.display = 'none';
                }
            }

            const container = document.getElementById('notificationsListContainer');
            if (container) {
                if (!data.notifications || data.notifications.length === 0) {
                    container.innerHTML = '<div class="p-3 text-center text-muted small"><i class="bi bi-bell-slash fs-4 d-block mb-1"></i> No new notifications</div>';
                    return;
                }

                let html = '<div class="list-group list-group-flush">';
                data.notifications.forEach(n => {
                    const unreadClass = n.isRead ? '' : 'bg-light fw-bold';
                    html += `
                        <div class="list-group-item p-2 border-bottom ${unreadClass}" style="font-size: 0.8rem;">
                            <div class="d-flex align-items-center justify-content-between mb-1">
                                <span class="badge bg-primary-subtle text-primary" style="font-size: 0.65rem;">${n.type}</span>
                                <small class="text-muted" style="font-size: 0.65rem;">${new Date(n.createdAt).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'})}</small>
                            </div>
                            <div class="text-dark mb-1">${n.title}</div>
                            <small class="text-muted d-block">${n.message}</small>
                        </div>
                    `;
                });
                html += '</div>';
                container.innerHTML = html;
            }
        }).catch(() => {});
}

function markAllNotificationsRead() {
    fetch('/api/notifications/mark-all-read', { method: 'POST' })
        .then(() => fetchUserNotifications())
        .catch(err => console.error(err));
}

document.addEventListener('DOMContentLoaded', () => {
    initNotificationsBell();
});

/* ==========================================================================
   Live Chat Assistant Widget
   ========================================================================== */
function toggleLiveChatModal() {
    const chatBox = document.getElementById('liveChatBoxWindow');
    const openIcon = document.getElementById('liveChatIconOpen');
    const closeIcon = document.getElementById('liveChatIconClose');

    if (!chatBox) return;

    if (chatBox.classList.contains('d-none')) {
        chatBox.classList.remove('d-none');
        if (openIcon) openIcon.classList.add('d-none');
        if (closeIcon) closeIcon.classList.remove('d-none');
    } else {
        chatBox.classList.add('d-none');
        if (openIcon) openIcon.classList.remove('d-none');
        if (closeIcon) closeIcon.classList.add('d-none');
    }
}

function sendLiveChatMessage() {
    const input = document.getElementById('liveChatInput');
    if (!input || !input.value.trim()) return;

    const userText = input.value.trim();
    input.value = '';

    appendLiveChatMessage('User', userText);

    fetch('/api/support/chat-bot', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ message: userText })
    })
    .then(res => res.json())
    .then(data => {
        if (data.reply) {
            appendLiveChatMessage('Agent', data.reply);
        }
    })
    .catch(() => {
        appendLiveChatMessage('Agent', 'Sorry, I am having trouble connecting right now. Please try again shortly!');
    });
}

function sendQuickChatMessage(text) {
    const input = document.getElementById('liveChatInput');
    if (input) {
        input.value = text;
        sendLiveChatMessage();
    }
}

function appendLiveChatMessage(sender, text) {
    const container = document.getElementById('liveChatMessagesList');
    if (!container) return;

    const div = document.createElement('div');
    if (sender === 'User') {
        div.className = 'd-flex align-items-end justify-content-end gap-2 ms-auto max-w-85';
        div.innerHTML = `<div class="bg-primary text-white rounded-3 p-2 small shadow-sm">${text}</div>`;
    } else {
        div.className = 'd-flex align-items-start gap-2 max-w-85 me-auto';
        div.innerHTML = `<div class="bg-white text-dark border rounded-3 p-2 small shadow-sm">${text}</div>`;
    }

    container.appendChild(div);
    container.scrollTop = container.scrollHeight;
}

/* ==========================================================================
   Support Ticket Handlers
   ========================================================================== */
function submitCustomerTicket(e) {
    e.preventDefault();
    const form = e.target;
    const formData = new FormData(form);
    const data = Object.fromEntries(formData.entries());

    fetch('/api/support/tickets/create', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data)
    })
    .then(res => res.json())
    .then(res => {
        if (res.success) {
            if (typeof showAuraToast === 'function') {
                showAuraToast(`Support Ticket #${res.ticketNumber} created successfully!`, 'success');
            }
            setTimeout(() => {
                window.location.href = `/account/tickets/${res.ticketId}`;
            }, 1000);
        } else {
            alert(res.message || 'Error submitting ticket.');
        }
    })
    .catch(err => console.error(err));
}

function submitTicketReply(e, ticketId) {
    e.preventDefault();
    const form = e.target;
    const messageInput = form.querySelector('textarea[name="Message"]');
    if (!messageInput || !messageInput.value.trim()) return;

    fetch('/api/support/tickets/reply', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ ticketId: ticketId, message: messageInput.value.trim() })
    })
    .then(res => res.json())
    .then(res => {
        if (res.success) {
            window.location.reload();
        } else {
            alert(res.message || 'Error sending reply.');
        }
    })
    .catch(err => console.error(err));
}
