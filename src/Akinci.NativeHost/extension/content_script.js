// Akıncı Downloader - Media & Live Stream Overlay Injector

(function () {
    let currentBadge = null;
    let detectedStreams = new Map(); // url -> metadata

    chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
        if (message.type === "AKINCI_MEDIA_DETECTED") {
            handleMediaDetected(message.streamUrl, message.tabTitle || document.title);
            sendResponse({ received: true });
        }
    });

    function handleMediaDetected(streamUrl, title) {
        if (!streamUrl) return;

        // Çok sık tekrarlanan segment m3u8 isteklerini tekil tut
        if (detectedStreams.has(streamUrl)) {
            return;
        }

        detectedStreams.set(streamUrl, {
            url: streamUrl,
            title: title || document.title || "Canlı Akış",
            detectedAt: Date.now()
        });

        renderBadge(streamUrl, title);
    }

    function renderBadge(streamUrl, title) {
        if (currentBadge && document.body.contains(currentBadge)) {
            return; // Zaten ekranda gösteriliyor
        }

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

        document.body.appendChild(container);
        currentBadge = container;

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
                dismissBadge();
            }, 3000);
        });

        dismissBtn.addEventListener("click", () => {
            dismissBadge();
        });
    }

    function dismissBadge() {
        if (currentBadge && document.body.contains(currentBadge)) {
            currentBadge.style.transition = "opacity 0.25s ease, transform 0.25s ease";
            currentBadge.style.opacity = "0";
            currentBadge.style.transform = "translateY(-10px) scale(0.95)";
            setTimeout(() => {
                if (currentBadge && document.body.contains(currentBadge)) {
                    document.body.removeChild(currentBadge);
                }
                currentBadge = null;
            }, 250);
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
})();
