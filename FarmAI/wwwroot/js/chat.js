(() => {
  'use strict';
  const dock = document.querySelector('[data-chat-dock]');
  const launcher = document.querySelector('[data-chat-launcher]');
  const close = document.querySelector('[data-chat-close]');
  if (dock && launcher) {
    const toggle = (show) => {
      dock.hidden = !show;
      launcher.setAttribute('aria-expanded', String(show));
      if (show) dock.querySelector('[data-chat-input]')?.focus();
    };
    launcher.addEventListener('click', () => toggle(dock.hidden));
    close?.addEventListener('click', () => toggle(false));
    document.addEventListener('keydown', e => { if(e.key === 'Escape' && !dock.hidden) toggle(false); });
  }
  for (const root of document.querySelectorAll('[data-chat-root]')) {
    const form = root.querySelector('[data-chat-form]');
    const input = root.querySelector('[data-chat-input]');
    const log = root.querySelector('[data-chat-messages]');
    const status = root.querySelector('[data-chat-status]');
    const send = root.querySelector('[data-chat-send]');
    const history = [];
    let busy = false;
    const addMessage = (message, kind, data) => {
      const bubble = document.createElement('div');
      bubble.className = 'chat-bubble ' + (kind === 'user' ? 'chat-user' : kind === 'error' ? 'chat-error' : 'chat-bot');
      bubble.textContent = message; // Never innerHTML for model output.
      if (data) {
        const meta = document.createElement('div');
        meta.className = 'chat-meta';
        meta.textContent = 'Nguồn xử lý: ' + data.provider + ' · ' + (data.model || '') + ' · Prompt ' + data.promptVersion;
        bubble.appendChild(meta);
        if (Array.isArray(data.sources) && data.sources.length) {
          const refs = document.createElement('div'); refs.className = 'chat-sources';
          refs.append('Dữ liệu nguồn: ');
          for (const source of data.sources) {
            if (typeof source.url !== 'string' || !source.url.startsWith('/') || source.url.startsWith('//')) continue;
            const a = document.createElement('a');
            a.href = source.url; a.textContent = source.label; a.target = '_self';
            refs.appendChild(a);
          }
          bubble.appendChild(refs);
        }
        if(Array.isArray(data.limitations) && data.limitations.length) {
          const limits = document.createElement('small'); limits.className = 'chat-limitations';
          limits.textContent = 'Lưu ý: ' + data.limitations.join('; ');
          bubble.appendChild(limits);
        }
      }
      log.appendChild(bubble); log.scrollTop = log.scrollHeight;
    };
    form.addEventListener('submit', async (event) => {
      event.preventDefault();
      if (busy) return;
      const question = input.value.trim();
      if (!question || question.length > 800) return;
      busy = true; send.disabled = true; input.disabled = true;
      addMessage(question, 'user'); input.value = '';
      status.textContent = 'Đang xử lý câu hỏi…';
      const controller = new AbortController();
      const timeout = setTimeout(() => controller.abort(), 34000);
      try {
        const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const response = await fetch(form.action, {
          method: 'POST', credentials: 'same-origin',
          headers: { 'Content-Type':'application/json', 'RequestVerificationToken':token || '' },
          body: JSON.stringify({question, history: history.slice(-8)}), signal: controller.signal
        });
        if (response.redirected && response.url.includes('/Account/Login')) {
          throw new Error('Phiên đăng nhập đã hết hạn. Hãy đăng nhập lại.');
        }
        const data = await response.json().catch(() => ({}));
        if (!response.ok) throw new Error(data.message ||
          (response.status === 429 ? 'Bạn hỏi quá nhiều lần. Vui lòng đợi rồi thử lại.' :
           response.status === 400 ? 'Yêu cầu không hợp lệ. Hãy kiểm tra nội dung.' :
           'Không thể kết nối chatbot. Hãy tải lại trang và thử lại.'));
        if (!data.answer) throw new Error('Mô hình trả nội dung trống.');
        addMessage(data.answer, 'assistant', data);
        history.push({role:'user',content:question},{role:'assistant',content:data.answer});
        while(history.length > 8) history.shift();
        status.textContent = data.provider === 'RULE_BASED'
          ? 'Chế độ quy tắc (chưa kết nối mô hình AI).'
          : 'Đã trả lời · ' + data.provider;
      } catch (e) {
        addMessage(e.name === 'AbortError' ? 'AI xử lý quá lâu, vui lòng gửi lại sau.' : e.message, 'error');
        status.textContent = 'Có lỗi khi trả lời. Không có dữ liệu quản lý nào bị thay đổi.';
      } finally {
        clearTimeout(timeout); busy = false; input.disabled = false; send.disabled = false; input.focus();
      }
    });
    input.addEventListener('keydown', e => {
      if(e.key === 'Enter' && !e.shiftKey) {e.preventDefault();form.requestSubmit();}
    });
    root.querySelectorAll('[data-chat-question]').forEach(button => {
      button.addEventListener('click', () => { input.value = button.dataset.chatQuestion || ''; form.requestSubmit(); });
    });
    root.querySelector('[data-chat-reset]')?.addEventListener('click', () => {
      if(busy)return;
      history.length = 0;
      log.replaceChildren();
      addMessage('Đã xóa nội dung trò chuyện trên màn hình. Bạn có thể hỏi câu mới.', 'assistant');
      status.textContent = 'Không lưu lịch sử hội thoại vào cơ sở dữ liệu.';
    });
  }
})();
