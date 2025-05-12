// 等待DOM加载完成
document.addEventListener('DOMContentLoaded', function() {
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