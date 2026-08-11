(() => {
  const root = document.documentElement;
  const themeToggle = document.getElementById('theme-toggle');
  const navToggle = document.querySelector('.nav-toggle');
  const siteNav = document.getElementById('site-nav');
  const revealItems = [...document.querySelectorAll('[data-reveal]')];
  const navLinks = [...document.querySelectorAll('.site-nav a[href^="#"]')];
  const packageButtons = [...document.querySelectorAll('[data-package-filter]')];
  const packageCards = [...document.querySelectorAll('[data-package-kind]')];

  const preferredTheme = () => {
    const stored = localStorage.getItem('foundationkit-theme');
    if (stored === 'light' || stored === 'dark') return stored;
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  };

  const applyTheme = theme => {
    root.dataset.theme = theme;
    localStorage.setItem('foundationkit-theme', theme);
    const themeColor = document.querySelector('meta[name="theme-color"]');
    if (themeColor) themeColor.content = theme === 'dark' ? '#101219' : '#F7F8FC';
    if (themeToggle) themeToggle.setAttribute('aria-label', `Switch to ${theme === 'dark' ? 'light' : 'dark'} theme`);
  };

  applyTheme(preferredTheme());
  themeToggle?.addEventListener('click', () => applyTheme(root.dataset.theme === 'dark' ? 'light' : 'dark'));

  navToggle?.addEventListener('click', () => {
    const open = !siteNav?.classList.contains('open');
    siteNav?.classList.toggle('open', open);
    navToggle.setAttribute('aria-expanded', String(open));
  });

  navLinks.forEach(link => link.addEventListener('click', () => {
    siteNav?.classList.remove('open');
    navToggle?.setAttribute('aria-expanded', 'false');
  }));

  packageButtons.forEach(button => {
    button.addEventListener('click', () => {
      const filter = button.dataset.packageFilter;
      packageButtons.forEach(item => item.classList.toggle('active', item === button));
      packageCards.forEach(card => {
        card.hidden = filter !== 'all' && card.dataset.packageKind !== filter;
      });
    });
  });

  document.querySelectorAll('[data-copy]').forEach(button => {
    button.addEventListener('click', async () => {
      const value = button.getAttribute('data-copy') ?? '';
      const original = button.textContent;
      try {
        await navigator.clipboard.writeText(value.replaceAll('\\n', '\n'));
        button.textContent = 'Copied';
      } catch {
        button.textContent = 'Copy failed';
      }
      window.setTimeout(() => { button.textContent = original; }, 1400);
    });
  });

  if ('IntersectionObserver' in window) {
    const revealObserver = new IntersectionObserver(entries => {
      entries.forEach(entry => {
        if (!entry.isIntersecting) return;
        entry.target.classList.add('visible');
        revealObserver.unobserve(entry.target);
      });
    }, { threshold: 0.08 });
    revealItems.forEach(item => revealObserver.observe(item));

    const navObserver = new IntersectionObserver(entries => {
      entries.forEach(entry => {
        if (!entry.isIntersecting) return;
        const id = entry.target.id;
        navLinks.forEach(link => link.classList.toggle('active', link.getAttribute('href') === `#${id}`));
      });
    }, { rootMargin: '-18% 0px -72% 0px', threshold: 0 });

    navLinks.forEach(link => {
      const target = document.querySelector(link.getAttribute('href'));
      if (target) navObserver.observe(target);
    });
  } else {
    revealItems.forEach(item => item.classList.add('visible'));
  }

  const hydrateManifest = async () => {
    try {
      const response = await fetch('./portal-manifest.json', { cache: 'no-cache' });
      if (!response.ok) throw new Error(`HTTP ${response.status}`);
      const manifest = await response.json();

      document.querySelectorAll('[data-package-count]').forEach(node => { node.textContent = manifest.packageCount; });
      document.querySelectorAll('[data-test-count]').forEach(node => { node.textContent = manifest.verifiedTests; });
      document.querySelectorAll('[data-baseline]').forEach(node => { node.textContent = manifest.baseline; });
      document.querySelectorAll('[data-manifest-summary]').forEach(node => {
        node.textContent = `${manifest.framework} · Phase ${manifest.roadmapEndPhase} · ${manifest.packageCount} packages`;
      });
    } catch (error) {
      console.warn('FoundationKit Pages manifest unavailable.', error);
    }
  };

  hydrateManifest();
})();
