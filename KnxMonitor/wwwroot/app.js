const $ = (s) => document.querySelector(s);
const statusEl = $('#status');
const filterEl = $('#filter');
const tbody = document.querySelector('#messages tbody');
const exportA = $('#export');

let currentFilter = '';

async function fetchStatus(){
  const res = await fetch('/api/status');
  const j = await res.json();
  statusEl.textContent = `${j.connected ? '✓ Connected' : '✗ Disconnected'} — ${j.status} — ${j.count} messages`;
  if (j.filter && !currentFilter) { currentFilter = j.filter; filterEl.value = j.filter; }
}

function csvExportHref(){
  const u = new URL('/api/export', location.origin);
  if (currentFilter) u.searchParams.set('filter', currentFilter);
  return u.toString();
}

async function fetchMessages(){
  const u = new URL('/api/messages', location.origin);
  u.searchParams.set('take', '500');
  if (currentFilter) u.searchParams.set('filter', currentFilter);
  const res = await fetch(u);
  const items = await res.json();
  render(items);
}

function render(items){
  tbody.innerHTML='';
  for (const m of items){
    const tr = document.createElement('tr');
    tr.innerHTML = `
      <td class="time">${new Date(m.timestamp).toLocaleTimeString()}</td>
      <td><span class="badge ${m.type}">${m.type}</span></td>
      <td>${m.source}</td>
      <td class="group">${m.groupAddress}</td>
      <td>${m.value ?? ''}</td>
      <td>${m.priority}</td>
      <td>${m.dpt ?? ''}</td>
      <td>${m.description ?? ''}</td>`;
    tbody.appendChild(tr);
  }
  exportA.href = csvExportHref();
}

$('#applyFilter').addEventListener('click', async () =>{
  currentFilter = filterEl.value.trim();
  await fetch('/api/filter', { method: 'POST', headers: { 'Content-Type':'application/json' }, body: JSON.stringify({ filter: currentFilter })});
  await fetchMessages();
});
$('#clearFilter').addEventListener('click', async () =>{
  filterEl.value='';
  currentFilter='';
  await fetch('/api/filter', { method: 'POST', headers: { 'Content-Type':'application/json' }, body: JSON.stringify({ filter: '' })});
  await fetchMessages();
});

async function tick(){
  try { await fetchStatus(); await fetchMessages(); } catch {}
  setTimeout(tick, 1000);
}

tick();

