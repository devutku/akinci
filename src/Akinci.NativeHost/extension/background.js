// Akıncı Downloader Browser Integration Service Worker

const HOST_NAME = "com.akinci.downloader";
const IPC_ENDPOINT = "http://127.0.0.1:49500/add-download/";

// Sağ tık menüsü oluştur
chrome.runtime.onInstalled.addListener(() => {
  chrome.contextMenus.create({
    id: "akinci-download-link",
    title: "Akıncı ile İndir",
    contexts: ["link", "image", "video", "audio"]
  });
});

// Sağ tık menüsüne tıklandığında
chrome.contextMenus.onClicked.addListener((info, tab) => {
  if (info.menuItemId === "akinci-download-link") {
    const targetUrl = info.linkUrl || info.srcUrl;
    if (targetUrl) {
      sendToAkinci(targetUrl, tab?.url);
    }
  }
});

// Tarayıcının standart indirme yöneticisini Akıncı'ya yönlendir (isteğe bağlı)
chrome.downloads.onCreated.addListener(async (downloadItem) => {
  // Akıncı'ya yönlendirildikten sonra tarayıcı indirmesini iptal etmek için:
  // sendToAkinci(downloadItem.url, downloadItem.referrer, downloadItem.filename);
  // chrome.downloads.cancel(downloadItem.id);
});

async function sendToAkinci(url, referrer, customFileName) {
  const payload = {
    url: url,
    referrer: referrer || "",
    customFileName: customFileName || null
  };

  // 1. Önce Native Messaging Host üzerinden dene
  try {
    chrome.runtime.sendNativeMessage(HOST_NAME, payload, (response) => {
      if (chrome.runtime.lastError || !response || response.status !== "success") {
        console.warn("[Akıncı] Native Host yanıt vermedi, doğrudan HTTP IPC deneniyor:", chrome.runtime.lastError?.message);
        fallbackHttpIpc(payload);
      } else {
        console.log("[Akıncı] Başarıyla aktarıldı:", response);
      }
    });
  } catch (err) {
    fallbackHttpIpc(payload);
  }
}

async function fallbackHttpIpc(payload) {
  try {
    const res = await fetch(IPC_ENDPOINT, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    });
    if (res.ok) {
      console.log("[Akıncı] HTTP IPC ile başarıyla kuyruğa alındı.");
    } else {
      console.error("[Akıncı] HTTP IPC hatası:", res.statusText);
    }
  } catch (e) {
    console.error("[Akıncı] Akıncı masaüstü uygulaması çalışmıyor olabilir:", e);
  }
}
