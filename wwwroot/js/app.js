// SmartCampus — Client-side helpers

// Auto-dismiss alerts after 5 seconds
document.addEventListener('DOMContentLoaded', () => {
    const alerts = document.querySelectorAll('.sc-alert-error');
    alerts.forEach(alert => {
        setTimeout(() => {
            alert.style.transition = 'opacity 0.4s ease';
            alert.style.opacity = '0';
            setTimeout(() => alert.remove(), 400);
        }, 5000);
    });
});

// Blazor JS interop helper — called from C# if needed
window.SmartCampus = {
    focusElement: (id) => {
        const el = document.getElementById(id);
        if (el) el.focus();
    },
    scrollToTop: () => window.scrollTo({ top: 0, behavior: 'smooth' })
};