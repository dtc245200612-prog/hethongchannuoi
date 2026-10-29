// FarmAI – only controls the mobile sidebar; does not change business workflows.
(() => {
  const button = document.querySelector('[data-menu-toggle]');
  const sidebar = document.querySelector('#farm-sidebar');
  const overlay = document.querySelector('[data-sidebar-overlay]');
  if (!button || !sidebar || !overlay) return;
  const close = () => {
    document.body.classList.remove('nav-open');
    overlay.hidden = true;
    button.setAttribute('aria-expanded', 'false');
    button.setAttribute('aria-label', 'Mở menu điều hướng');
  };
  const open = () => {
    document.body.classList.add('nav-open');
    overlay.hidden = false;
    button.setAttribute('aria-expanded', 'true');
    button.setAttribute('aria-label', 'Đóng menu điều hướng');
  };
  button.addEventListener('click', () => document.body.classList.contains('nav-open') ? close() : open());
  overlay.addEventListener('click', close);
  sidebar.querySelectorAll('a').forEach(link => link.addEventListener('click', close));
  document.addEventListener('keydown', event => { if (event.key === 'Escape') close(); });
  window.matchMedia('(min-width: 961px)').addEventListener('change', event => { if (event.matches) close(); });
})();
