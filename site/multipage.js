(() => {
  const current = location.pathname.split('/').pop() || 'index.html';
  const nav = document.getElementById('site-nav');
  const navToggle = document.querySelector('.nav-toggle');

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
})();
