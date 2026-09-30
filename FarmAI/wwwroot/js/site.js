(() => {
  const sidebar = document.querySelector('.sidebar');
  const backdrop = document.querySelector('.sidebar-backdrop');
  const toggle = document.querySelector('[data-menu-toggle]');
  if (toggle && sidebar && backdrop) {
    toggle.addEventListener('click', () => {
      sidebar.classList.toggle('open');
      backdrop.classList.toggle('show');
    });
    backdrop.addEventListener('click', () => {
      sidebar.classList.remove('open');
      backdrop.classList.remove('show');
    });
  }

  document.querySelectorAll('[data-confirm]').forEach(el => {
    el.addEventListener('click', e => {
      if (!confirm(el.dataset.confirm || 'Bạn chắc chắn muốn thực hiện?')) e.preventDefault();
    });
  });

  document.querySelectorAll('[data-tab]').forEach(btn => {
    btn.addEventListener('click', () => {
      const root = btn.closest('[data-tabs]');
      if (!root) return;
      root.querySelectorAll('[data-tab]').forEach(x => x.classList.remove('active'));
      root.querySelectorAll('[data-pane]').forEach(x => x.classList.remove('active'));
      btn.classList.add('active');
      root.querySelector(`[data-pane="${btn.dataset.tab}"]`)?.classList.add('active');
    });
  });
})();

window.FarmCharts = {
  line(canvas, rows) {
    if (!canvas || !rows || rows.length === 0) return;
    const ratio = window.devicePixelRatio || 1;
    const cssWidth = canvas.clientWidth || 900;
    const cssHeight = 320;
    canvas.width = cssWidth * ratio;
    canvas.height = cssHeight * ratio;
    const ctx = canvas.getContext('2d');
    ctx.scale(ratio, ratio);
    const w = cssWidth, h = cssHeight;
    const p = { l: 66, r: 35, t: 42, b: 52 };
    const vals = rows.map(x => Number(x.value));
    let min = Math.min(...vals), max = Math.max(...vals);
    if (min === max) { min -= 1; max += 1; }
    const pad = Math.max((max - min) * .25, 1);
    min -= pad; max += pad;

    ctx.clearRect(0, 0, w, h);
    ctx.font = '15px Georgia, "Times New Roman", serif';
    ctx.textBaseline = 'middle';
    ctx.strokeStyle = '#dfe8ef';
    ctx.lineWidth = 1.5;
    for (let i = 0; i < 5; i++) {
      const y = p.t + i * (h - p.t - p.b) / 4;
      ctx.beginPath(); ctx.moveTo(p.l, y); ctx.lineTo(w - p.r, y); ctx.stroke();
    }
    const points = rows.map((r, i) => ({
      ...r,
      x: rows.length === 1 ? (p.l + w - p.r) / 2 : p.l + i * (w - p.l - p.r) / (rows.length - 1),
      y: h - p.b - (Number(r.value) - min) / (max - min) * (h - p.t - p.b)
    }));
    ctx.strokeStyle = '#0b86bd'; ctx.lineWidth = 5; ctx.lineJoin = 'round'; ctx.lineCap = 'round';
    ctx.beginPath(); points.forEach((pt, i) => i ? ctx.lineTo(pt.x, pt.y) : ctx.moveTo(pt.x, pt.y)); ctx.stroke();
    points.forEach(pt => {
      ctx.fillStyle = '#0b86bd'; ctx.beginPath(); ctx.arc(pt.x, pt.y, 6, 0, Math.PI * 2); ctx.fill();
      ctx.fillStyle = '#52677c'; ctx.textAlign = 'center';
      ctx.fillText(`${Number(pt.value).toFixed(1)} kg`, pt.x, pt.y - 20);
      ctx.fillText(pt.label, pt.x, h - 20);
    });
  }
};

// Trợ lý AI: gọi endpoint JSON bất đồng bộ để trang không bị reload/đứng khi chờ OpenAI.
(() => {
  const form = document.querySelector('#ai-analysis-form');
  if (!form || !window.fetch) return;

  const button = document.querySelector('#ai-submit');
  const label = button?.querySelector('.ai-submit-label');
  const loading = button?.querySelector('.ai-submit-loading');
  const status = document.querySelector('#ai-request-status');
  const resultPanel = document.querySelector('#ai-result-panel');
  const endpoint = form.dataset.aiEndpoint || '/api/ai/summarize-herd';

  const setBusy = busy => {
    if (button) button.disabled = busy;
    if (label) label.hidden = busy;
    if (loading) loading.hidden = !busy;
    form.setAttribute('aria-busy', busy ? 'true' : 'false');
  };

  const addList = (root, title, items) => {
    if (!Array.isArray(items) || items.length === 0) return;
    const block = document.createElement('div');
    const heading = document.createElement('h4');
    heading.textContent = title;
    const ul = document.createElement('ul');
    items.forEach(item => {
      const li = document.createElement('li');
      li.textContent = String(item ?? '');
      ul.appendChild(li);
    });
    block.append(heading, ul);
    root.appendChild(block);
  };

  const renderResult = data => {
    if (!resultPanel) return;
    resultPanel.replaceChildren();

    const card = document.createElement('div');
    card.className = 'ai-result-card';

    const providerWrap = document.createElement('div');
    const provider = document.createElement('span');
    provider.className = data.usedExternalAi ? 'pill pill-ai' : 'pill';
    provider.textContent = data.provider || 'Kết quả AI';
    providerWrap.appendChild(provider);
    card.appendChild(providerWrap);

    const summaryBlock = document.createElement('div');
    const title = document.createElement('h4');
    title.textContent = data.title || 'Kết quả phân tích';
    const summary = document.createElement('p');
    summary.textContent = data.summary || 'Không có nội dung trả về.';
    summaryBlock.append(title, summary);
    card.appendChild(summaryBlock);

    addList(card, 'Điểm đáng chú ý', data.highlights);
    addList(card, 'Nhắc việc', data.reminders);
    addList(card, 'Cần theo dõi', data.warnings);

    const disclaimer = document.createElement('div');
    disclaimer.className = 'ai-disclaimer';
    disclaimer.textContent = 'Khuyến cáo: Kết quả do AI tạo ra chỉ mang tính chất tham khảo, không thay thế cho chỉ định của bác sĩ thú y.';
    card.appendChild(disclaimer);

    resultPanel.appendChild(card);
  };

  form.addEventListener('submit', async event => {
    event.preventDefault();
    if (button?.disabled) return;

    const task = document.querySelector('#ai-task')?.value || 'summary';
    const question = document.querySelector('#ai-question')?.value || '';
    const started = performance.now();
    const controller = new AbortController();
    const clientTimeout = window.setTimeout(() => controller.abort(), 25000);

    setBusy(true);
    if (status) {
      status.className = 'ai-request-status working';
      status.textContent = 'Đang lấy dữ liệu trang trại và gửi yêu cầu tới AI… Bạn vẫn có thể cuộn/xem trang.';
    }

    try {
      const response = await fetch(endpoint, {
        method: 'POST',
        credentials: 'same-origin',
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json',
          'X-FarmAI-Request': '1'
        },
        body: JSON.stringify({ task, question }),
        signal: controller.signal
      });

      if (response.status === 401 || response.status === 403) {
        throw new Error('Phiên đăng nhập đã hết hạn hoặc tài khoản không có quyền dùng AI.');
      }

      const contentType = response.headers.get('content-type') || '';
      const data = contentType.includes('application/json') ? await response.json() : null;
      if (!response.ok) {
        const message = data?.error || `API nội bộ trả về HTTP ${response.status}.`;
        throw new Error(message);
      }

      renderResult(data || {});
      const elapsed = ((performance.now() - started) / 1000).toFixed(1);
      if (status) {
        status.className = `ai-request-status ${data?.usedExternalAi ? 'success' : 'warning'}`;
        status.textContent = data?.usedExternalAi
          ? `OpenAI đã phản hồi sau ${elapsed} giây. Nhật ký KT3 đã được ghi tự động.`
          : `Đã hoàn tất sau ${elapsed} giây bằng chế độ fallback. Xem phần kết quả để biết nguyên nhân OpenAI không được dùng.`;
      }
    } catch (error) {
      const aborted = error?.name === 'AbortError';
      if (status) {
        status.className = 'ai-request-status error';
        status.textContent = aborted
          ? 'Yêu cầu phía trình duyệt đã quá 25 giây và được dừng để tránh treo giao diện. Hãy kiểm tra mạng/API key/quota.'
          : `Không thể gọi chức năng AI: ${error?.message || 'Lỗi không xác định'}`;
      }
      if (resultPanel) {
        resultPanel.innerHTML = '<div class="empty-state">Không nhận được kết quả. Trang vẫn hoạt động bình thường; hãy kiểm tra trạng thái phía trên rồi thử lại.</div>';
      }
    } finally {
      window.clearTimeout(clientTimeout);
      setBusy(false);
    }
  });
})();

// Prompt Lab: ba prompt được backend chạy song song; hiển thị trạng thái ngay khi submit.
(() => {
  const form = document.querySelector('#prompt-lab-form');
  const button = document.querySelector('#prompt-lab-submit');
  if (!form || !button) return;
  const label = button.querySelector('.prompt-lab-label');
  const loading = button.querySelector('.prompt-lab-loading');
  form.addEventListener('submit', () => {
    button.disabled = true;
    if (label) label.hidden = true;
    if (loading) loading.hidden = false;
    form.setAttribute('aria-busy', 'true');
  });
})();
