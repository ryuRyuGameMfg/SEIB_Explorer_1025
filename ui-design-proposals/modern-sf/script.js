// Panel switching functionality
document.addEventListener('DOMContentLoaded', function() {
    const tabButtons = document.querySelectorAll('.tab-btn');
    const panelContents = document.querySelectorAll('.panel-content');

    tabButtons.forEach(button => {
        button.addEventListener('click', function() {
            const panelId = this.getAttribute('data-panel');
            const targetPanel = document.getElementById(`panel-${panelId}`);
            if (!targetPanel) return;

            const currentPanel = document.querySelector('.panel-content.active');

            // Update tab button states
            tabButtons.forEach(btn => btn.classList.remove('active'));
            this.classList.add('active');

            // Deactivate current panel
            if (currentPanel && currentPanel !== targetPanel) {
                currentPanel.classList.remove('active');
            }

            // Activate target panel with animation
            if (!targetPanel.classList.contains('active')) {
                targetPanel.classList.add('active');
            }
            targetPanel.classList.remove('fade-in');
            void targetPanel.offsetWidth; // force reflow for animation restart
            targetPanel.classList.add('fade-in');

            // Reset scroll position inside sidebar
            const panelWrapper = document.querySelector('.panel-content-wrapper');
            if (panelWrapper) {
                panelWrapper.scrollTop = 0;
            }
        });
    });

    // Timeline slider functionality
    const timelineSlider = document.getElementById('timelineSlider');
    const yearDisplay = document.getElementById('yearDisplay');
    
    if (timelineSlider && yearDisplay) {
        function updateYearDisplay(value) {
            const year = String(Math.floor(value)).padStart(4, '0');
            yearDisplay.textContent = `Year ${year}`;
        }
        
        // Initialize display
        updateYearDisplay(timelineSlider.value);
        
        // Update on slider change
        timelineSlider.addEventListener('input', function() {
            updateYearDisplay(this.value);
        });
    }

    // Theme toggle functionality
    const themeToggle = document.getElementById('theme-toggle');
    const themeStylesheet = document.getElementById('theme-stylesheet');
    const themeLabel = document.getElementById('theme-label');
    const themeIcon = document.getElementById('theme-icon');
    
    function getCurrentTheme() {
        return localStorage.getItem('theme') || 'dark';
    }
    
    function setTheme(theme) {
        if (theme === 'light') {
            themeStylesheet.href = 'style-light.css';
            themeLabel.textContent = 'Dark Mode';
            // Update icon to moon
            themeIcon.innerHTML = `
                <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z" class="icon-stroke"></path>
            `;
        } else {
            themeStylesheet.href = 'style.css';
            themeLabel.textContent = 'Light Mode';
            // Update icon to sun
            themeIcon.innerHTML = `
                <circle cx="12" cy="12" r="5" class="icon-stroke"></circle>
                <line x1="12" y1="1" x2="12" y2="3" class="icon-stroke"></line>
                <line x1="12" y1="21" x2="12" y2="23" class="icon-stroke"></line>
                <line x1="4.22" y1="4.22" x2="5.64" y2="5.64" class="icon-stroke"></line>
                <line x1="18.36" y1="18.36" x2="19.78" y2="19.78" class="icon-stroke"></line>
                <line x1="1" y1="12" x2="3" y2="12" class="icon-stroke"></line>
                <line x1="21" y1="12" x2="23" y2="12" class="icon-stroke"></line>
                <line x1="4.22" y1="19.78" x2="5.64" y2="18.36" class="icon-stroke"></line>
                <line x1="18.36" y1="5.64" x2="19.78" y2="4.22" class="icon-stroke"></line>
            `;
        }
        localStorage.setItem('theme', theme);
    }
    
    // Initialize theme
    setTheme(getCurrentTheme());
    
    // Toggle theme on button click
    if (themeToggle) {
        themeToggle.addEventListener('click', function() {
            const currentTheme = getCurrentTheme();
            const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
            setTheme(newTheme);
        });
    }
});

