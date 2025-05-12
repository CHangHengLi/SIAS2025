// 等待DOM加载完成
document.addEventListener('DOMContentLoaded', function() {
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
    
    // 波浪参数 - 增加振幅和降低波浪位置使其更加可见
    const waves = [
        { y: height * 0.5, length: 0.008, amplitude: 30, speed: 0.005, color: getComputedStyle(document.documentElement).getPropertyValue('--wave-color-1') },
        { y: height * 0.45, length: 0.01, amplitude: 25, speed: 0.003, color: getComputedStyle(document.documentElement).getPropertyValue('--wave-color-2') },
        { y: height * 0.55, length: 0.012, amplitude: 35, speed: 0.002, color: getComputedStyle(document.documentElement).getPropertyValue('--wave-color-1') },
        { y: height * 0.6, length: 0.014, amplitude: 20, speed: 0.004, color: getComputedStyle(document.documentElement).getPropertyValue('--wave-color-2') }
    ];
    
    // 动画状态
    let animationTime = 0;
    
    // 绘制波浪
    function drawWaves() {
        ctx.clearRect(0, 0, width, height);
        
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
    
    // Hero区域动态光效果
    const hero = document.querySelector('.hero');
    if (hero) {
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
});
