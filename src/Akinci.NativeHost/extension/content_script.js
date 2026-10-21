// Akıncı Downloader - Media & Link Copy Overlay Content Script

(function () {
    let overlayRoot = null;
    let currentMediaBadge = null;
    let currentCopyBadge = null;
    let detectedStreams = new Map();

    let lastPromptedUrl = null;
    let lastPromptedTime = 0;

    let lastContextMenuUrl = null;
    let lastContextMenuTime = 0;

    // Ortak Overlay Kök Kapsayıcısını Hazırla (Sağ Üst)
    function ensureOverlayRoot() {
        if (!overlayRoot || !document.body.contains(overlayRoot)) {
            overlayRoot = document.getElementById("akinci-overlay-root");
            if (!overlayRoot) {
                overlayRoot = document.createElement("div");
                overlayRoot.id = "akinci-overlay-root";
                document.body.appendChild(overlayRoot);
            }
        }
        return overlayRoot;
    }

    // 1. Akış / Medya Dinleyicisi
    chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
        if (message.type === "AKINCI_MEDIA_DETECTED") {
            handleMediaDetected(message.streamUrl, message.tabTitle || document.title);
            sendResponse({ received: true });
        }
    });

    function handleMediaDetected(streamUrl, title) {
        if (!streamUrl || detectedStreams.has(streamUrl)) {
            return;
        }

        detectedStreams.set(streamUrl, {
            url: streamUrl,
            title: title || document.title || "Canlı Akış",
            detectedAt: Date.now()
        });

        renderMediaBadge(streamUrl, title);
    }

    function renderMediaBadge(streamUrl, title) {
        if (currentMediaBadge && document.body.contains(currentMediaBadge)) {
            return;
        }

        const root = ensureOverlayRoot();
        const container = document.createElement("div");
        container.className = "akinci-media-badge-container";
        container.id = "akinci-stream-overlay";

        const isLive = !streamUrl.includes(".mp4") && (streamUrl.includes(".m3u8") || streamUrl.includes(".mpd"));
        const displayTitle = (title || document.title || "Canlı Yayın").trim();

        container.innerHTML = `
            <div class="akinci-media-badge">
                ${isLive ? '<span class="akinci-pulse-dot" title="Canlı Akış Algılandı"></span>' : ''}
                <button class="akinci-media-btn" id="akinci-trigger-download" title="Bu medyayı Akıncı ile İndir">
                    <svg class="akinci-media-icon" viewBox="0 0 24 24">
                        <path d="M19.35 10.04C18.67 6.59 15.64 4 12 4 9.11 4 6.6 5.64 5.35 8.04 2.34 8.36 0 10.91 0 14c0 3.31 2.69 6 6 6h13c2.76 0 5-2.24 5-5 0-2.64-2.05-4.78-4.65-4.96zM17 13l-5 5-5-5h3V9h4v4h3z"/>
                    </svg>
                    <span>${isLive ? "Yayını İndir" : "Videoyu İndir"}</span>
                </button>
                <span class="akinci-media-title" title="${escapeHtml(displayTitle)}">${escapeHtml(displayTitle)}</span>
                <button class="akinci-close-btn" id="akinci-dismiss-btn" title="Kapat">✕</button>
            </div>
        `;

        root.appendChild(container);
        currentMediaBadge = container;

        const downloadBtn = container.querySelector("#akinci-trigger-download");
        const dismissBtn = container.querySelector("#akinci-dismiss-btn");

        downloadBtn.addEventListener("click", () => {
            downloadBtn.classList.add("akinci-success");
            downloadBtn.innerHTML = `
                <svg class="akinci-media-icon" viewBox="0 0 24 24">
                    <path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z"/>
                </svg>
                <span>Akıncı'ya Aktarıldı</span>
            `;

            chrome.runtime.sendMessage({
                type: "AKINCI_DOWNLOAD_STREAM",
                streamUrl: streamUrl,
                title: displayTitle
            });

            setTimeout(() => {
                dismissElement(container, () => {
                    currentMediaBadge = null;
                });
            }, 2500);
        });

        dismissBtn.addEventListener("click", () => {
            dismissElement(container, () => {
                currentMediaBadge = null;
            });
        });
    }

    // 2. Kopyalanan Bağlantı (Clipboard Copy) Algılayıcı ve Sağ Üst Pop-up
    function isValidDownloadUrl(text) {
        if (!text || typeof text !== "string") return false;
        text = text.trim();
        if (text.length < 8 || text.length > 2048 || /\s/.test(text)) return false;

        try {
            const u = new URL(text);
            if (u.protocol !== "http:" && u.protocol !== "https:" && u.protocol !== "ftp:") {
                return false;
            }
            if (u.hostname === "localhost" || u.hostname === "127.0.0.1" || u.hostname === "::1") {
                return false;
            }
            return true;
        } catch {
            return false;
        }
    }

    function extractFileNameFromUrl(url) {
        try {
            const u = new URL(url);
            const pathname = u.pathname;
            let filename = pathname.substring(pathname.lastIndexOf("/") + 1);
            if (filename) {
                filename = decodeURIComponent(filename);
                if (filename.length > 0) return filename;
            }
            return u.hostname + (u.pathname.length > 1 ? u.pathname : "");
        } catch {
            return url.length > 35 ? url.substring(0, 32) + "..." : url;
        }
    }

    function handleDetectedCopiedUrl(url) {
        if (!isValidDownloadUrl(url)) return;

        url = url.trim();

        // Son 5 saniye içinde aynı link için zaten bildirim gösterildiyse tekrarlama
        const now = Date.now();
        if (url === lastPromptedUrl && (now - lastPromptedTime) < 5000) {
            return;
        }

        lastPromptedUrl = url;
        lastPromptedTime = now;

        renderCopyBadge(url);
    }

    function renderCopyBadge(url) {
        const root = ensureOverlayRoot();

        // Önceki kopyalama rozeti varsa temizle
        if (currentCopyBadge && document.body.contains(currentCopyBadge)) {
            dismissElement(currentCopyBadge, () => {
                showNewCopyBadge(root, url);
            });
        } else {
            showNewCopyBadge(root, url);
        }
    }

    function showNewCopyBadge(root, url) {
        const container = document.createElement("div");
        container.className = "akinci-media-badge-container";
        container.id = "akinci-copy-overlay";

        const displayFileName = extractFileNameFromUrl(url);

        container.innerHTML = `
            <div class="akinci-copy-badge">
                <div class="akinci-copy-icon-wrap" title="Akıncı Downloader">
                    <svg class="akinci-media-icon" viewBox="0 0 24 24">
                        <path d="M19.35 10.04C18.67 6.59 15.64 4 12 4 9.11 4 6.6 5.64 5.35 8.04 2.34 8.36 0 10.91 0 14c0 3.31 2.69 6 6 6h13c2.76 0 5-2.24 5-5 0-2.64-2.05-4.78-4.65-4.96zM17 13l-5 5-5-5h3V9h4v4h3z"/>
                    </svg>
                </div>
                <div class="akinci-copy-details">
                    <span class="akinci-copy-tag">Bağlantı Algılandı</span>
                    <span class="akinci-media-title" title="${escapeHtml(url)}">${escapeHtml(displayFileName)}</span>
                </div>
                <button class="akinci-media-btn" id="akinci-trigger-copy-download" title="Bu bağlantıyı Akıncı ile İndir">
                    <span>Akıncı ile İndir</span>
                </button>
                <button class="akinci-close-btn" id="akinci-dismiss-copy-btn" title="Kapat">✕</button>
                <div class="akinci-progress-track">
                    <div class="akinci-progress-bar"></div>
                </div>
            </div>
        `;

        root.appendChild(container);
        currentCopyBadge = container;

        const downloadBtn = container.querySelector("#akinci-trigger-copy-download");
        const dismissBtn = container.querySelector("#akinci-dismiss-copy-btn");
        const badgeElem = container.querySelector(".akinci-copy-badge");

        let dismissTimeout = null;
        let isHovered = false;

        function startDismissTimer(durationMs) {
            clearTimeout(dismissTimeout);
            dismissTimeout = setTimeout(() => {
                if (!isHovered) {
                    dismissElement(container, () => {
                        if (currentCopyBadge === container) {
                            currentCopyBadge = null;
                        }
                    });
                }
            }, durationMs);
        }

        // 6 saniyelik otomatik kapanma süresi
        startDismissTimer(6000);

        badgeElem.addEventListener("mouseenter", () => {
            isHovered = true;
            clearTimeout(dismissTimeout);
        });

        badgeElem.addEventListener("mouseleave", () => {
            isHovered = false;
            startDismissTimer(3000); // Fare çekildiğinde 3 saniye sonra kapat
        });

        downloadBtn.addEventListener("click", () => {
            clearTimeout(dismissTimeout);
            downloadBtn.classList.add("akinci-success");
            downloadBtn.innerHTML = `
                <svg class="akinci-media-icon" viewBox="0 0 24 24">
                    <path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z"/>
                </svg>
                <span>Akıncı'ya Aktarıldı</span>
            `;

            chrome.runtime.sendMessage({
                type: "AKINCI_DOWNLOAD_URL",
                url: url,
                title: displayFileName,
                referrer: window.location.href
            });

            setTimeout(() => {
                dismissElement(container, () => {
                    if (currentCopyBadge === container) {
                        currentCopyBadge = null;
                    }
                });
            }, 1800);
        });

        dismissBtn.addEventListener("click", () => {
            clearTimeout(dismissTimeout);
            dismissElement(container, () => {
                if (currentCopyBadge === container) {
                    currentCopyBadge = null;
                }
            });
        });
    }

    function dismissElement(elem, callback) {
        if (elem && elem.parentNode) {
            elem.style.transition = "opacity 0.25s ease, transform 0.25s ease";
            elem.style.opacity = "0";
            elem.style.transform = "translateY(-12px) scale(0.95)";
            setTimeout(() => {
                if (elem && elem.parentNode) {
                    elem.parentNode.removeChild(elem);
                }
                if (callback) callback();
            }, 250);
        } else {
            if (callback) callback();
        }
    }

    function escapeHtml(str) {
        return (str || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    // 3. Tarayıcı Olaylarını Dinle: Kopyalama & Sağ Tık Bağlantı Kopyalama
    // A) Metin Seçip Kopyalama (Ctrl+C / Menüden Kopyala)
    document.addEventListener("copy", () => {
        // 1. Önce seçili metni kontrol et
        const selectedText = window.getSelection()?.toString()?.trim();
        if (isValidDownloadUrl(selectedText)) {
            handleDetectedCopiedUrl(selectedText);
            return;
        }

        // 2. Clipboard API desteği varsa kısa gecikmeyle panoyu kontrol et
        if (navigator.clipboard && navigator.clipboard.readText) {
            setTimeout(async () => {
                try {
                    const clipText = await navigator.clipboard.readText();
                    if (isValidDownloadUrl(clipText)) {
                        handleDetectedCopiedUrl(clipText);
                    }
                } catch {
                    // Pano erişimi engellendiyse sessizce geç
                }
            }, 60);
        }
    }, true);

    // B) Sağ Tık Menüsü ile Bağlantı Kopyalama ("Bağlantı adresini kopyala")
    document.addEventListener("contextmenu", (e) => {
        const anchor = e.target.closest("a");
        if (anchor && anchor.href && isValidDownloadUrl(anchor.href)) {
            lastContextMenuUrl = anchor.href;
            lastContextMenuTime = Date.now();
        }
    }, true);

    // Tarayıcı sağ tık menüsünden "Bağlantı adresini kopyala" seçilip sayfaya geri dönüldüğünde focus tetiklenir
    window.addEventListener("focus", async () => {
        const timeSinceRightClick = Date.now() - lastContextMenuTime;
        if (lastContextMenuUrl && timeSinceRightClick < 4000) {
            if (navigator.clipboard && navigator.clipboard.readText) {
                try {
                    const clipText = await navigator.clipboard.readText();
                    if (clipText && (clipText.trim() === lastContextMenuUrl.trim() || isValidDownloadUrl(clipText))) {
                        handleDetectedCopiedUrl(clipText.trim());
                    }
                } catch {
                    // İzin gerekirse veya erişilemezse
                }
            }
        }
    });

    // C) Sayfa içi Javascript ile panoya kopyalama yapıldığında yakalama
    if (navigator.clipboard && navigator.clipboard.writeText) {
        try {
            const originalWriteText = navigator.clipboard.writeText.bind(navigator.clipboard);
            navigator.clipboard.writeText = async function (text) {
                const res = await originalWriteText(text);
                if (isValidDownloadUrl(text)) {
                    handleDetectedCopiedUrl(text);
                }
                return res;
            };
        } catch {
            // Read-only prototype durumunda yoksay
        }
    }
})();
