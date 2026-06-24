window.downloadFileFromStream = async (fileName, contentStreamReference) => {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    const url = URL.createObjectURL(blob);
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName ?? '';
    anchorElement.click();
    anchorElement.remove();
    URL.revokeObjectURL(url);
};

window.kaaimanDateTime = {
    getTimezoneOffsetMinutes: () => new Date().getTimezoneOffset()
};

window.kaaimanUnsavedGuard = {
    _enabled: false,
    _dotNetRef: null,

    _beforeUnloadHandler: function (e) {
        e.preventDefault();
        e.returnValue = '';
        return '';
    },

    _clickHandler: function (e) {
        const guard = window.kaaimanUnsavedGuard;
        if (!guard._enabled || !guard._dotNetRef) return;

        // Only intercept a plain left-click without modifier keys.
        if (e.defaultPrevented || e.button !== 0 || e.ctrlKey || e.metaKey || e.shiftKey || e.altKey) return;

        const anchor = e.target.closest && e.target.closest('a[href]');
        if (!anchor) return;

        // Let new-tab, downloads and non-navigational links proceed normally.
        if (anchor.target && anchor.target !== '' && anchor.target !== '_self') return;
        if (anchor.hasAttribute('download')) return;

        const href = anchor.getAttribute('href');
        if (!href || href.startsWith('#') || href.startsWith('mailto:') || href.startsWith('tel:') || href.startsWith('javascript:')) return;

        const url = new URL(anchor.href, window.location.href);
        // Only guard same-origin navigations; external links rely on the beforeunload prompt.
        if (url.origin !== window.location.origin) return;
        // Ignore links to the current page (no navigation away).
        if (url.pathname === window.location.pathname && url.search === window.location.search) return;

        e.preventDefault();
        e.stopPropagation();
        guard._dotNetRef.invokeMethodAsync('OnGuardedNavigationAsync', url.href);
    },

    setEnabled: function (enabled, dotNetRef) {
        if (enabled) {
            this._dotNetRef = dotNetRef;
        }

        if (enabled === this._enabled) return;
        this._enabled = enabled;

        if (enabled) {
            window.addEventListener('beforeunload', this._beforeUnloadHandler);
            document.addEventListener('click', this._clickHandler, true);
        } else {
            window.removeEventListener('beforeunload', this._beforeUnloadHandler);
            document.removeEventListener('click', this._clickHandler, true);
            this._dotNetRef = null;
        }
    }
};
