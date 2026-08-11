(() => {
  const current = location.pathname.split('/').pop() || 'index.html';
  const nav = document.getElementById('site-nav');
  const navToggle = document.querySelector('.nav-toggle');
  const languageKey = 'foundationkit-language';

  document.querySelectorAll('.site-nav a[data-page]').forEach(link => {
    const target = link.getAttribute('href')?.split('#')[0] || '';
    link.classList.toggle('active', target === current);
    link.addEventListener('click', () => {
      nav?.classList.remove('open');
      navToggle?.setAttribute('aria-expanded', 'false');
    });
  });

  document.querySelectorAll('[data-year]').forEach(node => {
    node.textContent = String(new Date().getFullYear());
  });

  if (!document.querySelector('link[href="creative.css"]')) {
    const creative = document.createElement('link');
    creative.rel = 'stylesheet';
    creative.href = 'creative.css';
    document.head.appendChild(creative);
  }

  const commonArabic = new Map([
    ['Overview', 'نظرة عامة'], ['Architecture', 'المعمارية'], ['Capabilities', 'القدرات'], ['Packages', 'الحزم'],
    ['Frontend', 'الواجهة'], ['Quality', 'الجودة'], ['Start', 'ابدأ'], ['Developer', 'المطور'],
    ['Composable .NET Application Foundation', 'أساس .NET قابل للتركيب وإعادة الاستخدام'], ['Skip to content', 'انتقل إلى المحتوى'],
    ['Open navigation', 'فتح التنقل'], ['Start locally', 'ابدأ محليًا'], ['Explore Composer', 'استكشف Composer'],
    ['Reusable packages', 'حزم قابلة لإعادة الاستخدام'], ['Verified tests', 'اختبارات موثقة'],
    ['Roadmap end phase', 'نهاية خارطة الطريق'], ['Composition profiles', 'ملفات التركيب'],
    ['EXPLORE THE CORE', 'استكشف الكور'], ['One system. Clear rooms.', 'نظام واحد. مساحات واضحة.'],
    ['Getting Started', 'البدء'], ['Developer page', 'صفحة المطور'],
    ['Open architecture →', 'افتح المعمارية ←'], ['Explore capabilities →', 'استكشف القدرات ←'],
    ['Browse packages →', 'تصفح الحزم ←'], ['Enter Composer →', 'ادخل Composer ←'],
    ['See frontend system →', 'شاهد نظام الواجهة ←'], ['View evidence →', 'شاهد الأدلة ←'],
    ['Start first project →', 'ابدأ أول مشروع ←'], ['Open developer profile →', 'افتح ملف المطور ←'],
    ['Compose', 'تركيب'], ['Generate', 'توليد'], ['Persist', 'حفظ'], ['Read', 'قراءة'], ['Transport', 'نقل العقد'], ['Present', 'عرض'],
    ['Manifest intent', 'نية الـManifest'], ['Product solution', 'حل المشروع'], ['Entities + tables', 'Entities + Tables'],
    ['SQL views', 'SQL Views'], ['Soft Orbit UI', 'واجهة Soft Orbit']
  ]);

  function normalizeLanguage(value) {
    return String(value || '').toLowerCase() === 'ar' ? 'ar' : 'en';
  }

  function readLanguage() {
    try {
      return normalizeLanguage(localStorage.getItem(languageKey) || document.documentElement.lang || 'en');
    } catch {
      return normalizeLanguage(document.documentElement.lang || 'en');
    }
  }

  function persistLanguage(language) {
    try { localStorage.setItem(languageKey, language); } catch { /* best effort */ }
  }

  function captureOriginalText() {
    const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT, {
      acceptNode(node) {
        if (!node.nodeValue?.trim()) return NodeFilter.FILTER_REJECT;
        const parent = node.parentElement;
        if (!parent || ['SCRIPT', 'STYLE', 'CODE', 'PRE'].includes(parent.tagName)) return NodeFilter.FILTER_REJECT;
        if (parent.closest('[data-en][data-ar]')) return NodeFilter.FILTER_REJECT;
        return NodeFilter.FILTER_ACCEPT;
      }
    });
    const nodes = [];
    while (walker.nextNode()) {
      const node = walker.currentNode;
      node.__foundationOriginalText = node.nodeValue;
      nodes.push(node);
    }
    return nodes;
  }

  const commonTextNodes = captureOriginalText();

  function translateCommon(language) {
    commonTextNodes.forEach(node => {
      const original = node.__foundationOriginalText || node.nodeValue || '';
      if (language !== 'ar') {
        node.nodeValue = original;
        return;
      }
      const trimmed = original.trim();
      const translated = commonArabic.get(trimmed);
      if (!translated) {
        node.nodeValue = original;
        return;
      }
      const leading = original.match(/^\s*/)?.[0] || '';
      const trailing = original.match(/\s*$/)?.[0] || '';
      node.nodeValue = `${leading}${translated}${trailing}`;
    });
  }

  function applyLocalizedAttributes(language) {
    document.querySelectorAll('[data-en][data-ar]').forEach(element => {
      const value = language === 'ar' ? element.dataset.ar : element.dataset.en;
      if (typeof value === 'string') element.textContent = value;
    });
    document.querySelectorAll('[data-placeholder-en][data-placeholder-ar]').forEach(element => {
      element.setAttribute('placeholder', language === 'ar' ? element.dataset.placeholderAr : element.dataset.placeholderEn);
    });
    document.querySelectorAll('[data-aria-en][data-aria-ar]').forEach(element => {
      element.setAttribute('aria-label', language === 'ar' ? element.dataset.ariaAr : element.dataset.ariaEn);
    });
  }

  const topActions = document.querySelector('.top-actions');
  let languageButton = document.querySelector('[data-language-toggle]');
  if (!languageButton && topActions) {
    languageButton = document.createElement('button');
    languageButton.type = 'button';
    languageButton.className = 'language-toggle';
    languageButton.dataset.languageToggle = '';
    topActions.insertBefore(languageButton, topActions.firstChild);
  }

  function applyLanguage(language, persist = true) {
    const normalized = normalizeLanguage(language);
    document.documentElement.lang = normalized;
    document.documentElement.dir = normalized === 'ar' ? 'rtl' : 'ltr';
    document.documentElement.dataset.language = normalized;
    document.body.dataset.language = normalized;
    applyLocalizedAttributes(normalized);
    translateCommon(normalized);
    if (languageButton) {
      const targetArabic = normalized !== 'ar';
      languageButton.textContent = targetArabic ? 'ع' : 'EN';
      languageButton.title = targetArabic ? 'التبديل إلى العربية' : 'Switch to English';
      languageButton.setAttribute('aria-label', languageButton.title);
    }
    if (persist) persistLanguage(normalized);
    document.dispatchEvent(new CustomEvent('foundationkit:languagechange', { detail: { language: normalized } }));
    return normalized;
  }

  languageButton?.addEventListener('click', () => {
    applyLanguage(document.documentElement.lang === 'ar' ? 'en' : 'ar');
  });

  applyLanguage(readLanguage(), false);
})();
