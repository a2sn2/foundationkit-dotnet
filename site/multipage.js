(() => {
  const current = location.pathname.split('/').pop() || 'index.html';
  document.querySelectorAll('.site-nav a[data-page]').forEach(link => {
    const target = link.getAttribute('href')?.split('#')[0] || '';
    link.classList.toggle('active', target === current);
  });

  document.querySelectorAll('[data-year]').forEach(node => {
    node.textContent = String(new Date().getFullYear());
  });
})();
