// Akıncı Downloader Browser Integration Service Worker (Silent Interceptor)

const HOST_NAME = "com.akinci.downloader";
const IPC_ENDPOINT = "http://127.0.0.1:49500/add-download/";

// IDM-benzeri otomatik yakalanacak dosya uzantıları
const TARGET_EXTENSIONS = [
  // Arşivler & Disk Kalıpları
  ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".iso", ".img", ".vmdk",
  // Yürütülebilir / Kurulum Paketleri
  ".exe", ".msi", ".bat", ".cmd", ".apk", ".dmg", ".pkg", ".deb", ".rpm",
  // Medya & Video / Ses
  ".mp4", ".mkv", ".avi", ".mov", ".flv", ".webm", ".mp3", ".wav", ".flac", ".aac",
  // Büyük Belgeler & İkili Dosyalar
  ".pdf", ".bin", ".rom"
];

// Dosya adı veya URL eşleşiyor mu kontrolü
function shouldIntercept(filename, url) {
  const target = (filename || url || "").toLowerCase().split("?")[0].split("#")[0];
  return TARGET_EXTENSIONS.some(ext => target.endsWith(ext));
}

// 1. Sağ tık menüsü
chrome.runtime.onInstalled.addListener(() => {
  chrome.contextMenus.create({
    id: "akinci-download-link",
    title: "Akıncı ile İndir",
    contexts: ["link", "image", "video", "audio"]
  });
});

chrome.contextMenus.onClicked.addListener((info, tab) => {
  if (info.menuItemId === "akinci-download-link") {
    const targetUrl = info.linkUrl || info.srcUrl;
    if (targetUrl) {
      sendToAkinci(targetUrl, tab?.url);
    }
  }
});

// 2. Sessiz Otomatik Yakalama (Determining Filename aşamasında yakalama)
// Tarayıcı dosyayı diske yazmaya başlamadan hemen önce devreye girer
if (chrome.downloads && chrome.downloads.onDeterminingFilename) {
  chrome.downloads.onDeterminingFilename.addListener((downloadItem, suggest) => {
    if (shouldIntercept(downloadItem.filename, downloadItem.url)) {
      // Tarayıcının kendi indirmesini mikrosaniyede sessizce iptal et
      chrome.downloads.cancel(downloadItem.id, () => {
        // İndirme çubuğundaki kalıntıyı temizle
        chrome.downloads.erase({ id: downloadItem.id });
      });

      // Doğrudan Akıncı'ya aktar
      sendToAkinci(downloadItem.url, downloadItem.referrer, downloadItem.filename);
      return;
    }
    suggest();
  });
} else if (chrome.downloads && chrome.downloads.onCreated) {
  // Eski API / Fallback desteği
  chrome.downloads.onCreated.addListener((downloadItem) => {
    if (shouldIntercept(downloadItem.filename, downloadItem.url)) {
      chrome.downloads.cancel(downloadItem.id, () => {
        chrome.downloads.erase({ id: downloadItem.id });
      });
      sendToAkinci(downloadItem.url, downloadItem.referrer, downloadItem.filename);
    }
  });
}

// 3. Akıncı Motoruna İletme (Native Messaging Host veya HTTP IPC)
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
