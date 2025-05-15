// 等待DOM加载完成
document.addEventListener('DOMContentLoaded', function() {
    // 功能检查函数 
    function checkFunctionalityStatus() {
        // 检查轮播图功能
        const carousel = document.querySelector('.carousel-slides');
        const slides = document.querySelectorAll('.carousel-slide');
        const prevButton = document.querySelector('.carousel-button.prev');
        const nextButton = document.querySelector('.carousel-button.next');
        const indicators = document.querySelectorAll('.indicator');
        
        if (!carousel) console.error('轮播图容器未找到');
        if (slides.length === 0) console.error('轮播图幻灯片未找到');
        if (!prevButton) console.error('轮播图上一张按钮未找到');
        if (!nextButton) console.error('轮播图下一张按钮未找到');
        if (indicators.length === 0) console.error('轮播图指示器未找到');
        
        // 检查图片放大功能
        const lightbox = document.getElementById('lightbox');
        const lightboxImg = document.getElementById('lightbox-img');
        const lightboxCaption = document.querySelector('.lightbox-caption');
        const closeLightbox = document.querySelector('.close-lightbox');
        const screenshotImages = document.querySelectorAll('.screenshot-img');
        
        if (!lightbox) console.error('图片放大蒙版未找到');
        if (!lightboxImg) console.error('图片放大蒙版图片元素未找到');
        if (!lightboxCaption) console.error('图片放大蒙版标题未找到');
        if (!closeLightbox) console.error('图片放大蒙版关闭按钮未找到');
        if (screenshotImages.length === 0) console.error('系统截图图片未找到');
        
        console.log('功能检查完成，如果没有错误信息，则功能应该正常工作');
    }
    
    // 在页面加载后延迟500ms运行检查，确保DOM已完全加载
    setTimeout(checkFunctionalityStatus, 500);
    
    // 检测安装包下载按钮点击
    const downloadButtons = document.querySelectorAll('a[href="SIASGraduate.zip"]');
    downloadButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            // 更新为ZIP下载提示
            if(!confirm('您即将下载SIAS2025应用程序压缩包。下载后请解压缩并运行其中的SIASGraduate.exe文件。是否继续？')) {
                e.preventDefault();
            }
        });
    });
    
    // 水波纹背景效果
    const canvas = document.getElementById('background-canvas');
    const ctx = canvas.getContext('2d');
    let width = window.innerWidth;
    let height = window.innerHeight;
    
    // 调整画布大小以适应窗口
    function resizeCanvas() {
        width = window.innerWidth;
        height = window.innerHeight;
        canvas.width = width;
        canvas.height = height;
    }
    
    // 初始化调整画布大小
    resizeCanvas();
    
    // 监听窗口大小变化
    window.addEventListener('resize', resizeCanvas);
    
    // 波浪参数 - 明显增加振幅和调整波浪位置使其更加可见
    const waves = [
        { y: height * 0.75, length: 0.008, amplitude: 80, speed: 0.006, color: 'rgba(52, 152, 219, 0.9)' },
        { y: height * 0.7, length: 0.01, amplitude: 70, speed: 0.004, color: 'rgba(187, 222, 251, 0.95)' },
        { y: height * 0.8, length: 0.012, amplitude: 90, speed: 0.003, color: 'rgba(52, 152, 219, 0.85)' },
        { y: height * 0.85, length: 0.014, amplitude: 60, speed: 0.005, color: 'rgba(187, 222, 251, 0.9)' }
    ];
    
    // 动画状态
    let animationTime = 0;
    
    // 绘制波浪
    function drawWaves() {
        ctx.clearRect(0, 0, width, height);
        
        // 绘制渐变背景
        const gradient = ctx.createLinearGradient(0, 0, 0, height);
        gradient.addColorStop(0, 'rgba(230, 247, 255, 0.1)');
        gradient.addColorStop(1, 'rgba(187, 222, 251, 0.3)');
        ctx.fillStyle = gradient;
        ctx.fillRect(0, 0, width, height);
        
        waves.forEach(wave => {
            ctx.beginPath();
            
            // 绘制波浪路径 - 使用更小的步长使波浪更平滑
            for (let x = 0; x <= width; x += 0.5) {
                const dx = x * wave.length;
                const y = wave.y + Math.sin(dx + animationTime * wave.speed) * wave.amplitude;
                
                if (x === 0) {
                    ctx.moveTo(x, y);
                } else {
                    ctx.lineTo(x, y);
                }
            }
            
            // 完成波浪路径
            ctx.lineTo(width, height);
            ctx.lineTo(0, height);
            ctx.closePath();
            
            // 填充波浪
            ctx.fillStyle = wave.color;
            ctx.fill();
        });
        
        // 更新动画时间
        animationTime += 1;
        
        // 重绘下一帧
        requestAnimationFrame(drawWaves);
    }
    
    // 开始绘制
    drawWaves();
    
    // 添加鼠标跟踪聚焦背景效果
    const hero = document.querySelector('.hero');
    if (hero) {
        // 创建聚焦背景层
        const spotlightEffect = document.createElement('div');
        spotlightEffect.classList.add('spotlight-effect');
        hero.insertBefore(spotlightEffect, hero.firstChild); // 确保添加到hero的第一个子元素位置
        
        // 初始化聚焦点位置（默认中心点）
        let spotlightX = hero.offsetWidth / 2;
        let spotlightY = hero.offsetHeight / 2;
        
        // 更新聚焦点位置和效果
        function updateSpotlight(x, y) {
            requestAnimationFrame(() => {
                spotlightEffect.style.background = `radial-gradient(
                    circle at ${x}px ${y}px,
                    rgba(255, 255, 255, 0.8) 0%,
                    rgba(176, 224, 255, 0.6) 20%,
                    rgba(135, 206, 250, 0.4) 40%,
                    rgba(52, 152, 219, 0.2) 60%,
                    rgba(44, 62, 80, 0.2) 100%
                )`;
            });
        }
        
        // 鼠标移动事件
        hero.addEventListener('mousemove', function(e) {
            // 获取相对于hero元素的鼠标位置
            const rect = hero.getBoundingClientRect();
            spotlightX = e.clientX - rect.left;
            spotlightY = e.clientY - rect.top;
            
            // 更新聚焦点
            updateSpotlight(spotlightX, spotlightY);
        });
        
        // 初始化聚焦效果
        updateSpotlight(spotlightX, spotlightY);
        
        // 触摸设备支持
        hero.addEventListener('touchmove', function(e) {
            e.preventDefault();
            const touch = e.touches[0];
            const rect = hero.getBoundingClientRect();
            spotlightX = touch.clientX - rect.left;
            spotlightY = touch.clientY - rect.top;
            
            updateSpotlight(spotlightX, spotlightY);
        });
    }
    
    // Hero区域动态光效果
    if (hero) {
        // 清除可能存在的旧光点元素
        hero.querySelectorAll('.hero-light').forEach(light => light.remove());
        
        // 创建闪光点元素
        for (let i = 0; i < 20; i++) {
            const light = document.createElement('div');
            light.classList.add('hero-light');
            
            // 随机定位
            const size = Math.random() * 8 + 2; // 2-10px
            light.style.width = `${size}px`;
            light.style.height = `${size}px`;
            light.style.left = `${Math.random() * 100}%`;
            light.style.top = `${Math.random() * 100}%`;
            
            // 随机动画延迟
            light.style.animationDelay = `${Math.random() * 5}s`;
            light.style.animationDuration = `${Math.random() * 5 + 5}s`; // 5-10s
            
            hero.appendChild(light);
        }
    }
    
    // 平滑滚动效果
    const navLinks = document.querySelectorAll('#main-nav a, .cta-buttons a');
    
    navLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            // 确保链接指向页内锚点
            if (this.getAttribute('href').startsWith('#')) {
                e.preventDefault();
                
                const targetId = this.getAttribute('href');
                const targetElement = document.querySelector(targetId);
                
                if (targetElement) {
                    const navHeight = document.querySelector('#main-nav').offsetHeight;
                    const targetPosition = targetElement.getBoundingClientRect().top + window.pageYOffset;
                    const offsetPosition = targetPosition - navHeight - 20; // 额外偏移量
                    
                    window.scrollTo({
                        top: offsetPosition,
                        behavior: 'smooth'
                    });
                }
            }
        });
    });
    
    // 导航栏链接高亮
    function highlightNavLinks() {
        const sections = document.querySelectorAll('section[id]');
        const navHeight = document.querySelector('#main-nav').offsetHeight;
        
        let currentSection = '';
        
        sections.forEach(section => {
            const sectionTop = section.offsetTop - navHeight - 100;
            const sectionHeight = section.offsetHeight;
            const sectionId = section.getAttribute('id');
            
            if (window.scrollY >= sectionTop && window.scrollY < sectionTop + sectionHeight) {
                currentSection = sectionId;
            }
        });
        
        navLinks.forEach(link => {
            link.classList.remove('active');
            const href = link.getAttribute('href');
            if (href && href === `#${currentSection}`) {
                link.classList.add('active');
            }
        });
    }
    
    // 初始运行一次，确保刷新页面时高亮也正确
    highlightNavLinks();
    
    // 监听滚动事件
    window.addEventListener('scroll', function() {
        highlightNavLinks();
        toggleScrollTopButton();
        
        // 导航栏背景色变化
        const nav = document.getElementById('main-nav');
        if (window.scrollY > 100) {
            nav.classList.add('scrolled');
        } else {
            nav.classList.remove('scrolled');
        }
    });
    
    // 添加返回顶部按钮
    const body = document.querySelector('body');
    const scrollTopButton = document.createElement('button');
    scrollTopButton.innerHTML = '<i class="fas fa-arrow-up"></i>';
    scrollTopButton.classList.add('scroll-top-btn');
    scrollTopButton.setAttribute('title', '返回顶部');
    body.appendChild(scrollTopButton);
    
    // 显示/隐藏返回顶部按钮
    function toggleScrollTopButton() {
        if (window.scrollY > 500) {
            scrollTopButton.classList.add('show');
        } else {
            scrollTopButton.classList.remove('show');
        }
    }
    
    // 返回顶部按钮点击事件
    scrollTopButton.addEventListener('click', function() {
        window.scrollTo({
            top: 0,
            behavior: 'smooth'
        });
    });
    
    // 添加滚动动画
    const animatedElements = document.querySelectorAll('.feature-card, .tech-item, .security-item, .screenshot');
    
    function checkVisibility() {
        animatedElements.forEach(element => {
            const elementTop = element.getBoundingClientRect().top;
            const windowHeight = window.innerHeight;
            
            if (elementTop < windowHeight - 50) {
                element.classList.add('fade-in');
            }
        });
    }
    
    // 初始运行一次
    checkVisibility();
    
    // 监听滚动事件
    window.addEventListener('scroll', checkVisibility);
    
    // 添加CSS样式
    const style = document.createElement('style');
    style.textContent = `
        #main-nav.scrolled {
            background-color: rgba(44, 62, 80, 0.95);
            box-shadow: 0 5px 15px rgba(0, 0, 0, 0.1);
            backdrop-filter: blur(15px);
            -webkit-backdrop-filter: blur(15px);
        }
        
        #main-nav a.active {
            color: var(--secondary-color);
        }
        
        #main-nav a.active::after {
            width: 100%;
        }
        
        .scroll-top-btn {
            position: fixed;
            bottom: 30px;
            right: 30px;
            width: 50px;
            height: 50px;
            border-radius: 50%;
            background-color: var(--secondary-color);
            color: white;
            border: none;
            box-shadow: 0 3px 10px rgba(0, 0, 0, 0.2);
            cursor: pointer;
            opacity: 0;
            transform: translateY(20px);
            transition: all 0.3s ease;
            z-index: 999;
            display: flex;
            align-items: center;
            justify-content: center;
        }
        
        .scroll-top-btn i {
            font-size: 1.2rem;
        }
        
        .scroll-top-btn:hover {
            background-color: #2980b9;
            transform: translateY(0) scale(1.1);
        }
        
        .scroll-top-btn.show {
            opacity: 1;
            transform: translateY(0);
        }
        
        .feature-card, .tech-item, .security-item, .screenshot {
            opacity: 0;
            transform: translateY(30px);
            transition: opacity 0.5s ease, transform 0.5s ease;
        }
        
        .feature-card.fade-in, .tech-item.fade-in, .security-item.fade-in, .screenshot.fade-in {
            opacity: 1;
            transform: translateY(0);
        }
    `;
    document.head.appendChild(style);
    
    // 图片放大功能 - 修复
    const lightbox = document.getElementById('lightbox');
    const lightboxImg = document.getElementById('lightbox-img');
    const lightboxCaption = document.querySelector('.lightbox-caption');
    const closeLightbox = document.querySelector('.close-lightbox');
    const screenshotImages = document.querySelectorAll('.screenshot-img');
    
    // 确保所有元素都存在
    if (lightbox && lightboxImg && lightboxCaption && closeLightbox && screenshotImages.length > 0) {
        // 打开放大图片
        screenshotImages.forEach(img => {
            img.addEventListener('click', function() {
                lightbox.style.display = 'block';
                lightboxImg.src = this.getAttribute('data-full') || this.src;
                if (this.alt) {
                    lightboxCaption.textContent = this.alt;
                } else {
                    lightboxCaption.textContent = '';
                }
                
                // 防止背景滚动
                document.body.style.overflow = 'hidden';
            });
        });
        
        // 关闭放大图片
        closeLightbox.addEventListener('click', function() {
            lightbox.style.display = 'none';
            
            // 恢复背景滚动
            document.body.style.overflow = 'auto';
        });
        
        // 点击蒙版任意位置也可关闭
        lightbox.addEventListener('click', function(e) {
            if (e.target === lightbox) {
                lightbox.style.display = 'none';
                document.body.style.overflow = 'auto';
            }
        });
        
        // 按ESC键关闭图片
        document.addEventListener('keydown', function(e) {
            if (e.key === 'Escape' && lightbox.style.display === 'block') {
                lightbox.style.display = 'none';
                document.body.style.overflow = 'auto';
            }
        });
    } else {
        console.error('Lightbox elements not found');
    }
    
    // 轮播图功能 - 修复
    const carousel = document.querySelector('.carousel-slides');
    const slides = document.querySelectorAll('.carousel-slide');
    const prevButton = document.querySelector('.carousel-button.prev');
    const nextButton = document.querySelector('.carousel-button.next');
    const indicators = document.querySelectorAll('.indicator');
    
    // 确保所有轮播图元素都存在
    if (carousel && slides.length > 0 && prevButton && nextButton && indicators.length > 0) {
        let currentSlide = 0;
        const totalSlides = slides.length;
        
        // 初始化轮播图
        updateCarousel();
        
        // 更新轮播图显示
        function updateCarousel() {
            carousel.style.transform = `translateX(-${currentSlide * 100}%)`;
            
            // 更新指示器状态
            indicators.forEach((indicator, index) => {
                if (index === currentSlide) {
                    indicator.classList.add('active');
                } else {
                    indicator.classList.remove('active');
                }
            });
        }
        
        // 下一张幻灯片
        function nextSlide() {
            currentSlide = (currentSlide + 1) % totalSlides;
            updateCarousel();
        }
        
        // 上一张幻灯片
        function prevSlide() {
            currentSlide = (currentSlide - 1 + totalSlides) % totalSlides;
            updateCarousel();
        }
        
        // 按钮点击事件
        nextButton.addEventListener('click', nextSlide);
        prevButton.addEventListener('click', prevSlide);
        
        // 指示器点击事件
        indicators.forEach((indicator, index) => {
            indicator.addEventListener('click', () => {
                currentSlide = index;
                updateCarousel();
            });
        });
        
        // 自动播放
        let autoplayInterval = setInterval(nextSlide, 5000);
        
        // 鼠标悬停时暂停自动播放
        const carouselContainer = document.querySelector('.carousel-container');
        if (carouselContainer) {
            carouselContainer.addEventListener('mouseenter', () => {
                clearInterval(autoplayInterval);
            });
            
            // 鼠标离开时恢复自动播放
            carouselContainer.addEventListener('mouseleave', () => {
                autoplayInterval = setInterval(nextSlide, 5000);
            });
        }
        
        // 触摸滑动支持
        let touchStartX = 0;
        let touchEndX = 0;
        
        carousel.addEventListener('touchstart', e => {
            touchStartX = e.changedTouches[0].screenX;
        });
        
        carousel.addEventListener('touchend', e => {
            touchEndX = e.changedTouches[0].screenX;
            handleSwipe();
        });
        
        function handleSwipe() {
            // 检测滑动方向
            const swipeThreshold = 50; // 最小滑动距离
            if (touchEndX < touchStartX - swipeThreshold) {
                // 向左滑动 -> 下一张
                nextSlide();
            } else if (touchEndX > touchStartX + swipeThreshold) {
                // 向右滑动 -> 上一张
                prevSlide();
            }
        }
    } else {
        console.error('Carousel elements not found');
    }
});

// 添加window.onload事件，确保所有资源（包括图片）都已完全加载
window.onload = function() {
    console.log("所有资源已加载完毕，初始化轮播图和图片放大功能");
    
    // 轮播图功能 - 重新初始化
    initCarousel();
    
    // 图片放大功能 - 重新初始化
    initLightbox();
};

// 轮播图初始化函数
function initCarousel() {
    const carousel = document.querySelector('.carousel-slides');
    const slides = document.querySelectorAll('.carousel-slide');
    const prevButton = document.querySelector('.carousel-button.prev');
    const nextButton = document.querySelector('.carousel-button.next');
    const indicators = document.querySelectorAll('.indicator');
    
    // 确保所有轮播图元素都存在
    if (!carousel || slides.length === 0 || !prevButton || !nextButton || indicators.length === 0) {
        console.error('轮播图组件未完全加载，无法初始化轮播图');
        return;
    }
    
    let currentSlide = 0;
    const totalSlides = slides.length;
    
    // 更新轮播图显示
    function updateCarousel() {
        carousel.style.transform = `translateX(-${currentSlide * 100}%)`;
        
        // 更新指示器状态
        indicators.forEach((indicator, index) => {
            if (index === currentSlide) {
                indicator.classList.add('active');
            } else {
                indicator.classList.remove('active');
            }
        });
    }
    
    // 初始化轮播图
    updateCarousel();
    
    // 下一张幻灯片
    function nextSlide() {
        currentSlide = (currentSlide + 1) % totalSlides;
        updateCarousel();
    }
    
    // 上一张幻灯片
    function prevSlide() {
        currentSlide = (currentSlide - 1 + totalSlides) % totalSlides;
        updateCarousel();
    }
    
    // 按钮点击事件
    nextButton.addEventListener('click', nextSlide);
    prevButton.addEventListener('click', prevSlide);
    
    // 指示器点击事件
    indicators.forEach((indicator, index) => {
        indicator.addEventListener('click', () => {
            currentSlide = index;
            updateCarousel();
        });
    });
    
    // 自动播放
    let autoplayInterval = setInterval(nextSlide, 5000);
    
    // 鼠标悬停时暂停自动播放
    const carouselContainer = document.querySelector('.carousel-container');
    if (carouselContainer) {
        carouselContainer.addEventListener('mouseenter', () => {
            clearInterval(autoplayInterval);
        });
        
        // 鼠标离开时恢复自动播放
        carouselContainer.addEventListener('mouseleave', () => {
            autoplayInterval = setInterval(nextSlide, 5000);
        });
    }
    
    // 触摸滑动支持
    let touchStartX = 0;
    let touchEndX = 0;
    
    carousel.addEventListener('touchstart', e => {
        touchStartX = e.changedTouches[0].screenX;
    });
    
    carousel.addEventListener('touchend', e => {
        touchEndX = e.changedTouches[0].screenX;
        handleSwipe();
    });
    
    function handleSwipe() {
        // 检测滑动方向
        const swipeThreshold = 50; // 最小滑动距离
        if (touchEndX < touchStartX - swipeThreshold) {
            // 向左滑动 -> 下一张
            nextSlide();
        } else if (touchEndX > touchStartX + swipeThreshold) {
            // 向右滑动 -> 上一张
            prevSlide();
        }
    }
    
    console.log('轮播图功能已初始化');
}

// 图片放大功能初始化
function initLightbox() {
    const lightbox = document.getElementById('lightbox');
    const lightboxImg = document.getElementById('lightbox-img');
    const lightboxCaption = document.querySelector('.lightbox-caption');
    const closeLightbox = document.querySelector('.close-lightbox');
    const screenshotImages = document.querySelectorAll('.screenshot-img');
    
    // 确保所有元素都存在
    if (!lightbox || !lightboxImg || !lightboxCaption || !closeLightbox || screenshotImages.length === 0) {
        console.error('图片放大蒙版组件未完全加载，无法初始化图片放大功能');
        return;
    }
    
    // 打开放大图片
    screenshotImages.forEach(img => {
        img.addEventListener('click', function() {
            lightbox.style.display = 'block';
            lightboxImg.src = this.getAttribute('data-full') || this.src;
            if (this.alt) {
                lightboxCaption.textContent = this.alt;
            } else {
                lightboxCaption.textContent = '';
            }
            
            // 防止背景滚动
            document.body.style.overflow = 'hidden';
        });
    });
    
    // 关闭放大图片
    closeLightbox.addEventListener('click', function() {
        lightbox.style.display = 'none';
        
        // 恢复背景滚动
        document.body.style.overflow = 'auto';
    });
    
    // 点击蒙版任意位置也可关闭
    lightbox.addEventListener('click', function(e) {
        if (e.target === lightbox) {
            lightbox.style.display = 'none';
            document.body.style.overflow = 'auto';
        }
    });
    
    // 按ESC键关闭图片
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape' && lightbox.style.display === 'block') {
            lightbox.style.display = 'none';
            document.body.style.overflow = 'auto';
        }
    });
    
    console.log('图片放大功能已初始化');
}
