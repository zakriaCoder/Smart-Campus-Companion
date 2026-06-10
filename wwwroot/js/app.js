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
    scrollToTop: () => window.scrollTo({ top: 0, behavior: 'smooth' }),
    renderRideMap: (mapId, route) => {
        const el = document.getElementById(mapId);
        if (!el || !window.L || !route) return;

        if (el._smartCampusMap) {
            el._smartCampusMap.remove();
            el._smartCampusMap = null;
        }

        const origin = [route.originLat, route.originLon];
        const destination = [route.destinationLat, route.destinationLon];
        const geometry = Array.isArray(route.geometry) && route.geometry.length > 1
            ? route.geometry.map(p => [p.lat, p.lon])
            : [origin, destination];

        const map = L.map(el, {
            zoomControl: true,
            scrollWheelZoom: false
        });

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '&copy; OpenStreetMap contributors'
        }).addTo(map);

        L.marker(origin).addTo(map).bindPopup(`Pickup: ${route.pickup}`);
        L.marker(destination).addTo(map).bindPopup(`Destination: ${route.destination}`);

        const line = L.polyline(geometry, {
            color: '#0d9488',
            weight: 5,
            opacity: 0.9
        }).addTo(map);

        map.fitBounds(line.getBounds(), { padding: [28, 28] });
        el._smartCampusMap = map;
    }
};
