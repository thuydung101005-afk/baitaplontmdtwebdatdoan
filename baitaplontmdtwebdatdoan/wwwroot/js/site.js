document.addEventListener('DOMContentLoaded', function () {
    updateCartBadge();

    document.querySelectorAll('.qty-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const card = this.closest('.product-card');
            const qtySpan = card.querySelector('.qty-value');
            let qty = parseInt(qtySpan.textContent);
            if (this.dataset.action === 'increase') qty++;
            else qty = Math.max(1, qty - 1);
            qtySpan.textContent = qty;
        });
    });

    // Nút thêm giỏ hàng 1-click mượt mà
    document.querySelectorAll('.add-cart-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const card = this.closest('.product-card');
            const self = this;
            
            self.disabled = true;
            addToCart(card, () => {
                flyToCart(card);
                self.innerHTML = '<i class="fa-solid fa-check text-success"></i>';
                setTimeout(() => {
                    self.innerHTML = '<i class="fa-solid fa-cart-plus"></i>';
                    self.disabled = false;
                }, 1200);
            }, false, () => {
                self.disabled = false;
            });
        });
    });

    // Nút Mua ngay
    document.querySelectorAll('.buy-now-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const card = this.closest('.product-card');
            const id = card.dataset.id;
            const qtySpan = card.querySelector('.qty-value');
            const qty = qtySpan ? qtySpan.textContent : '1';

            function doBuyNow(forceNew) {
                fetch('/Cart/AddAjax', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: `menuItemId=${id}&quantity=${qty}&forceNewCart=${forceNew}&buyNow=true`
                })
                .then(res => {
                    if (res.status === 401) {
                        window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
                        return null;
                    }
                    return res.json();
                })
                .then(data => {
                    if (!data) return;
                    if (data.needConfirmNewCart) {
                        if (confirm(data.message)) {
                            doBuyNow(true);
                        }
                        return;
                    }
                    if (data.success) {
                        window.location.href = `/Order/Checkout?selectedIds=${data.cartItemId}`;
                    }
                })
                .catch(() => alert('Có lỗi xảy ra, vui lòng thử lại.'));
            }

            doBuyNow(false);
        });
    });
});

function addToCart(card, onSuccess, forceNewCart = false, onComplete) {
    const id = card.dataset.id;
    const qtySpan = card.querySelector('.qty-value');
    const qty = qtySpan ? qtySpan.textContent : '1';

    fetch('/Cart/AddAjax', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: `menuItemId=${id}&quantity=${qty}&forceNewCart=${forceNewCart}`
    })
    .then(res => {
        if (res.status === 401) {
            window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
            return null;
        }
        if (!res.ok) throw new Error('Request failed');
        return res.json();
    })
    .then(data => {
        if (!data) return;
        if (data.needConfirmNewCart) {
            if (confirm(data.message)) {
                addToCart(card, onSuccess, true, onComplete);
            } else if (onComplete) {
                onComplete();
            }
            return;
        }
        if (data.success) {
            setCartBadge(data.cartCount);
            if (onSuccess) onSuccess();
        }
    })
    .catch(() => {
        alert('Có lỗi xảy ra, vui lòng thử lại.');
        if (onComplete) onComplete();
    });
}

function flyToCart(card) {
    const img = card.querySelector('.product-img');
    const cartIcon = document.getElementById('cartIcon');
    if (!img || !cartIcon) return;

    const imgRect = img.getBoundingClientRect();
    const cartRect = cartIcon.getBoundingClientRect();

    const flyImg = img.cloneNode(true);
    flyImg.style.position = 'fixed';
    flyImg.style.left = imgRect.left + 'px';
    flyImg.style.top = imgRect.top + 'px';
    flyImg.style.width = imgRect.width + 'px';
    flyImg.style.height = imgRect.height + 'px';
    flyImg.style.borderRadius = '10px';
    flyImg.style.zIndex = 9999;
    flyImg.style.transition = 'all 0.7s cubic-bezier(0.55, 0, 1, 0.45)';
    flyImg.style.pointerEvents = 'none';
    document.body.appendChild(flyImg);

    requestAnimationFrame(() => {
        flyImg.style.left = (cartRect.left + cartRect.width / 2 - 10) + 'px';
        flyImg.style.top = (cartRect.top + cartRect.height / 2 - 10) + 'px';
        flyImg.style.width = '20px';
        flyImg.style.height = '20px';
        flyImg.style.opacity = '0.3';
    });

    flyImg.addEventListener('transitionend', () => {
        flyImg.remove();
        cartIcon.classList.add('cart-bounce');
        setTimeout(() => cartIcon.classList.remove('cart-bounce'), 300);
    });
}

function setCartBadge(count) {
    const badge = document.getElementById('cartCountBadge');
    if (!badge) return;
    badge.textContent = count;
    badge.style.display = count > 0 ? 'inline-block' : 'none';
}

function updateCartBadge() {
    fetch('/Cart/Count')
        .then(res => res.ok ? res.json() : null)
        .then(data => { if (data) setCartBadge(data.cartCount); })
        .catch(() => { });
}

function updateFavoriteBadge() {
    fetch('/Favorite/GetCount')
        .then(res => res.ok ? res.json() : null)
        .then(data => {
            if (data && typeof data.count !== 'undefined') {
                const badge = document.getElementById('favCountBadge');
                if (badge) {
                    badge.textContent = data.count;
                    badge.style.display = data.count > 0 ? 'inline-block' : 'none';
                }
            }
        })
        .catch(() => { });
}

// Global favorite button click handler
document.addEventListener('click', function (e) {
    const btn = e.target.closest('.toggle-fav-btn');
    if (!btn) return;
    e.preventDefault();
    e.stopPropagation();

    const restaurantId = btn.dataset.restaurantId;
    if (!restaurantId) return;

    fetch('/Favorite/Toggle', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: `restaurantId=${restaurantId}`
    })
    .then(res => {
        if (res.status === 401) {
            window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
            return null;
        }
        return res.json();
    })
    .then(data => {
        if (!data || !data.success) return;
        const icon = btn.querySelector('i');
        const textSpan = btn.querySelector('.fav-btn-text');
        if (data.isFavorite) {
            if (icon) {
                icon.classList.remove('fa-regular', 'text-secondary');
                icon.classList.add('fa-solid', 'text-danger');
            }
            if (textSpan) textSpan.textContent = 'Đã thích';
            btn.classList.add('is-favorited');
        } else {
            if (icon) {
                icon.classList.remove('fa-solid', 'text-danger');
                icon.classList.add('fa-regular');
            }
            if (textSpan) textSpan.textContent = 'Yêu thích';
            btn.classList.remove('is-favorited');
        }
        updateFavoriteBadge();
        showGlobalToast(data.message);
    })
    .catch(() => alert('Có lỗi xảy ra khi cập nhật yêu thích.'));
});

function showGlobalToast(message) {
    let toast = document.getElementById('foodieGlobalToast');
    if (!toast) {
        toast = document.createElement('div');
        toast.id = 'foodieGlobalToast';
        toast.style.cssText = 'position:fixed;bottom:24px;left:50%;transform:translateX(-50%);background:rgba(20,20,20,0.92);color:#fff;padding:12px 24px;border-radius:30px;font-size:14px;font-weight:600;z-index:99999;box-shadow:0 10px 25px rgba(0,0,0,0.3);transition:all 0.3s ease;opacity:0;pointer-events:none;';
        document.body.appendChild(toast);
    }
    toast.innerHTML = `<i class="fa-solid fa-heart me-2 text-danger"></i>${message}`;
    toast.style.opacity = '1';
    toast.style.bottom = '30px';
    clearTimeout(toast._timer);
    toast._timer = setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.bottom = '20px';
    }, 2500);
}

// Số tin nhắn chưa đọc trên icon tin nhắn ở thanh đầu trang
function updateChatBadge() {
    fetch('/Chat/UnreadCount')
        .then(res => res.ok ? res.json() : null)
        .then(data => {
            if (data && typeof data.count !== 'undefined') {
                const badge = document.getElementById('chatCountBadge');
                if (badge) {
                    badge.textContent = data.count;
                    badge.style.display = data.count > 0 ? 'inline-block' : 'none';
                }
            }
        })
        .catch(() => { });
}

document.addEventListener('DOMContentLoaded', function () {
    updateFavoriteBadge();
    updateChatBadge();
    setInterval(updateChatBadge, 30000);
});