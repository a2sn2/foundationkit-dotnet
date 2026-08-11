window.FoundationKitTheme = (() => {
    const storageKey = 'foundationkit-theme';
    const validThemes = new Set(['light', 'dark', 'auto']);

    function normalize(theme) {
        return validThemes.has(theme) ? theme : 'auto';
    }

    function apply(theme) {
        const normalized = normalize(theme);
        const root = document.documentElement;

        if (normalized === 'auto') {
            root.removeAttribute('data-fk-theme');
        } else {
            root.setAttribute('data-fk-theme', normalized);
        }

        root.dataset.fkThemePreference = normalized;
        return normalized;
    }

    function initialize() {
        let stored = 'auto';
        try {
            stored = localStorage.getItem(storageKey) || 'auto';
        } catch {
            stored = 'auto';
        }

        return apply(stored);
    }

    function currentResolved() {
        const preference = document.documentElement.dataset.fkThemePreference || 'auto';
        if (preference !== 'auto') {
            return preference;
        }

        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }

    function set(theme) {
        const normalized = apply(theme);
        try {
            localStorage.setItem(storageKey, normalized);
        } catch {
            // Theme persistence is best-effort. Rendering remains functional without storage.
        }
        return currentResolved();
    }

    function toggle() {
        return set(currentResolved() === 'dark' ? 'light' : 'dark');
    }

    function setDirection(direction) {
        const normalized = direction === 'rtl' ? 'rtl' : 'ltr';
        document.documentElement.dir = normalized;
        return normalized;
    }

    return {
        initialize,
        set,
        toggle,
        currentResolved,
        setDirection
    };
})();

window.FoundationKitTheme.initialize();
