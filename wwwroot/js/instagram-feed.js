/**
 * Instagram Feed SwiperJS & Analytics Tracking Integration
 * Account: @ebsor_infosystems
 */

document.addEventListener('DOMContentLoaded', function () {
    initInstagramSwiper();
});

function initInstagramSwiper() {
    const container = document.querySelector('.insta-swiper-container');
    if (!container) return;

    if (typeof Swiper === 'undefined') {
        console.warn('SwiperJS is not loaded. Retrying in 300ms...');
        setTimeout(initInstagramSwiper, 300);
        return;
    }

    const instaSwiper = new Swiper('.insta-swiper-container', {
        slidesPerView: 1.25,
        spaceBetween: 16,
        loop: true,
        grabCursor: true,
        centeredSlides: false,
        speed: 600,
        autoplay: {
            delay: 3500,
            disableOnInteraction: false,
            pauseOnMouseEnter: true
        },
        navigation: {
            nextEl: '.insta-swiper-next',
            prevEl: '.insta-swiper-prev'
        },
        keyboard: {
            enabled: true
        },
        breakpoints: {
            576: {
                slidesPerView: 2.2,
                spaceBetween: 16
            },
            768: {
                slidesPerView: 3.2,
                spaceBetween: 20
            },
            992: {
                slidesPerView: 4.2,
                spaceBetween: 24
            },
            1200: {
                slidesPerView: 5.2,
                spaceBetween: 24
            },
            1600: {
                slidesPerView: 6.2,
                spaceBetween: 28
            }
        }
    });
}

function trackInstagramClick(postId, permalink) {
    try {
        if (!postId) return;
        
        const payload = {
            postId: postId,
            permalink: permalink || 'https://www.instagram.com/ebsor_infosystems/'
        };

        if (navigator.sendBeacon) {
            const blob = new Blob([JSON.stringify(payload)], { type: 'application/json' });
            navigator.sendBeacon('/api/instagram/track-click', blob);
        } else {
            fetch('/api/instagram/track-click', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(payload),
                keepalive: true
            }).catch(function (err) {
                console.warn('Analytics logging notice:', err);
            });
        }
    } catch (e) {
        console.warn('Click track error:', e);
    }
}
