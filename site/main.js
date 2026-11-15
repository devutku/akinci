// Akıncı tanıtım sitesi: indirme bağlantılarını GitHub'daki en son sürümden günceller.
// GitHub API'ye ulaşılamazsa HTML'deki sabit (v1.1.0) bağlantılar olduğu gibi kalır.
(function () {
  'use strict';

  document.documentElement.classList.add('js');

  var REPO = 'devutku/akinci';
  var ASSET_PATTERNS = {
    'setup': /^Akinci-Setup-.*\.exe$/i,
    'win-zip': /-win-x64\.zip$/i,
    'linux': /-linux-x64\.tar\.gz$/i,
    'mac-arm': /-osx-arm64\.tar\.gz$/i,
    'mac-x64': /-osx-x64\.tar\.gz$/i
  };

  function formatSize(bytes) {
    return Math.round(bytes / 1048576) + ' MB';
  }

  function applyRelease(release) {
    if (!release || !release.tag_name || !Array.isArray(release.assets)) return;

    document.querySelectorAll('[data-release-version]').forEach(function (el) {
      el.textContent = release.tag_name;
    });
    document.querySelectorAll('[data-release-notes]').forEach(function (el) {
      el.href = release.html_url;
    });

    Object.keys(ASSET_PATTERNS).forEach(function (key) {
      var asset = release.assets.find(function (a) { return ASSET_PATTERNS[key].test(a.name); });
      if (!asset) return;
      document.querySelectorAll('[data-asset="' + key + '"]').forEach(function (el) {
        el.href = asset.browser_download_url;
      });
      document.querySelectorAll('[data-asset-size="' + key + '"]').forEach(function (el) {
        el.textContent = formatSize(asset.size);
      });
    });
  }

  if (window.fetch) {
    fetch('https://api.github.com/repos/' + REPO + '/releases/latest', { headers: { 'Accept': 'application/vnd.github+json' } })
      .then(function (r) { return r.ok ? r.json() : null; })
      .then(applyRelease)
      .catch(function () { /* sabit bağlantılar kalır */ });
  }

  // Bölümler görünür alana girerken hafifçe belirsin (hareket azaltma tercihine saygı)
  var reduceMotion = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  var targets = document.querySelectorAll('.feature, .step, .download-list li, .faq details');
  if (!reduceMotion && 'IntersectionObserver' in window) {
    targets.forEach(function (el, i) {
      el.classList.add('reveal');
      el.style.transitionDelay = (i % 6) * 40 + 'ms';
    });
    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (entry) {
        if (entry.isIntersecting) {
          entry.target.classList.add('is-visible');
          io.unobserve(entry.target);
        }
      });
    }, { rootMargin: '0px 0px -40px 0px' });
    targets.forEach(function (el) { io.observe(el); });
  }
})();
