(async () => {
  const target = document.getElementById('manifest');
  try {
    const response = await fetch('./portal-manifest.json');
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    const manifest = await response.json();
    target.textContent = `${manifest.product} · ${manifest.packageCount} reusable packages · ${manifest.baseline}`;
  } catch (error) {
    target.textContent = 'FoundationKit Core manifest unavailable.';
    console.error(error);
  }
})();
