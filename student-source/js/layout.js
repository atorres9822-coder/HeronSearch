(async function () {
  const isPublic = document.body.dataset.layout === 'public';

  async function load(id, file) {
    const el = document.getElementById(id);
    if (!el) return;
    try {
      const res = await fetch(file);
      el.innerHTML = await res.text();
    } catch (e) {
      console.error('Could not load ' + file, e);
    }
  }

  await Promise.all([
    load('site-header', isPublic ? '../components/header-public.html' : '../components/header.html'),
    load('site-footer', '../components/footer.html')
  ]);

  const here = location.pathname.split('/').pop() || 'landing.html';
  document.querySelectorAll('#site-header .nav-links a').forEach(function (a) {
    const pages = (a.dataset.match || a.getAttribute('href')).split(' ');
    if (pages.includes(here)) a.classList.add('active');
  });
})();