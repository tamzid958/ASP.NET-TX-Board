namespace AspNet.Tx.Board.Ui;

internal static class TxBoardUiPage
{
    public const string Html = """
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>Tx Board</title>
  <script src="https://cdn.tailwindcss.com"></script>
  <script src="https://code.jquery.com/jquery-3.7.1.min.js"></script>
</head>
<body class="min-h-screen bg-slate-100 text-slate-800">
  <div class="flex min-h-screen">
    <aside class="hidden w-72 flex-col bg-slate-900 text-slate-100 lg:flex">
      <div class="border-b border-slate-800 px-6 py-5">
        <h1 class="text-xl font-semibold tracking-tight">ASP.NET Tx Board</h1>
        <p class="mt-1 text-sm text-slate-400">Transaction monitoring dashboard</p>
      </div>
      <nav class="flex-1 space-y-2 p-4">
        <a href="#" class="block rounded-lg bg-slate-800 px-4 py-3 text-sm font-medium text-white">Transaction Board</a>
        <a href="#" class="block rounded-lg px-4 py-3 text-sm font-medium text-slate-300">Runtime Overview</a>
      </nav>
      <div class="border-t border-slate-800 px-6 py-4 text-xs text-slate-400">v0.1.0</div>
    </aside>

    <main class="w-full p-4 md:p-8">
      <div class="mx-auto max-w-7xl space-y-6">
        <header class="rounded-2xl bg-white p-5 shadow-sm ring-1 ring-slate-200">
          <div class="flex flex-wrap items-center justify-between gap-4">
            <div>
              <h2 class="text-2xl font-semibold tracking-tight">Transaction Monitoring</h2>
              <p class="text-sm text-slate-500">Similar to Spring Tx Board, adapted for ASP.NET with Tailwind and jQuery.</p>
            </div>
            <div class="flex items-center gap-3">
              <a href="/tx-board/api/export" target="_blank" class="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50">Export CSV</a>
              <button id="refreshBtn" class="rounded-lg bg-slate-900 px-4 py-2 text-sm font-medium text-white transition hover:bg-slate-700">Refresh</button>
            </div>
          </div>
        </header>

        <section class="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <article class="rounded-2xl bg-white p-5 shadow-sm ring-1 ring-slate-200">
            <p class="text-sm text-slate-500">Total Transactions</p>
            <p id="totalTransactions" class="mt-2 text-2xl font-semibold">0</p>
          </article>
          <article class="rounded-2xl bg-white p-5 shadow-sm ring-1 ring-slate-200">
            <p class="text-sm text-slate-500">Success Rate</p>
            <p id="successRate" class="mt-2 text-2xl font-semibold text-emerald-600">0%</p>
          </article>
          <article class="rounded-2xl bg-white p-5 shadow-sm ring-1 ring-slate-200">
            <p class="text-sm text-slate-500">Unhealthy</p>
            <p id="unhealthyCount" class="mt-2 text-2xl font-semibold text-rose-600">0</p>
          </article>
          <article class="rounded-2xl bg-white p-5 shadow-sm ring-1 ring-slate-200">
            <p class="text-sm text-slate-500">Avg Duration</p>
            <p id="avgDuration" class="mt-2 text-2xl font-semibold">0 ms</p>
          </article>
        </section>

        <section class="rounded-2xl bg-white p-5 shadow-sm ring-1 ring-slate-200">
          <div class="mb-4 flex flex-wrap items-center justify-between gap-4">
            <h3 class="text-lg font-semibold">Duration Distribution</h3>
            <button id="refreshDistributionBtn" class="rounded-lg border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-50">Reload</button>
          </div>
          <div id="distributionBars" class="space-y-3"></div>
        </section>

        <section class="rounded-2xl bg-white p-5 shadow-sm ring-1 ring-slate-200">
          <div class="mb-4 grid grid-cols-1 gap-3 md:grid-cols-3">
            <div>
              <label for="searchInput" class="mb-1 block text-xs font-medium uppercase text-slate-500">Search Method / Path</label>
              <input id="searchInput" type="text" class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm outline-none ring-slate-400 focus:ring" placeholder="OrderService.CreateOrder" />
            </div>
            <div>
              <label for="statusFilter" class="mb-1 block text-xs font-medium uppercase text-slate-500">Status</label>
              <select id="statusFilter" class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm outline-none ring-slate-400 focus:ring">
                <option value="">All</option>
                <option value="Committed">Committed</option>
                <option value="RolledBack">RolledBack</option>
                <option value="Errored">Errored</option>
              </select>
            </div>
            <div class="flex items-end gap-3">
              <label class="inline-flex items-center gap-2 text-sm">
                <input id="unhealthyOnly" type="checkbox" class="h-4 w-4 rounded border-slate-300 text-slate-900 focus:ring-slate-500" />
                Unhealthy only
              </label>
              <button id="clearFiltersBtn" class="rounded-lg border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50">Reset</button>
            </div>
          </div>

          <div class="overflow-x-auto rounded-xl border border-slate-200">
            <table class="min-w-full divide-y divide-slate-200 text-sm">
              <thead class="bg-slate-50">
                <tr class="text-left text-xs uppercase tracking-wide text-slate-500">
                  <th class="px-4 py-3">Method</th>
                  <th class="px-4 py-3">Path</th>
                  <th class="px-4 py-3">Status</th>
                  <th class="px-4 py-3">Duration</th>
                  <th class="px-4 py-3">Connections</th>
                  <th class="px-4 py-3">SQL Count</th>
                  <th class="px-4 py-3">Ended At</th>
                </tr>
              </thead>
              <tbody id="rows" class="divide-y divide-slate-100 bg-white"></tbody>
            </table>
          </div>
        </section>
      </div>
    </main>
  </div>

  <script>
    let allTransactions = [];

    function formatDate(value) {
      return value ? new Date(value).toLocaleString() : '-';
    }

    function renderSummary(items) {
      const total = items.length;
      const committed = items.filter(x => x.status === 'Committed').length;
      const unhealthy = items.filter(x => x.isUnhealthy).length;
      const avgDuration = total === 0 ? 0 : Math.round(items.reduce((sum, x) => sum + x.durationMs, 0) / total);

      $('#totalTransactions').text(total);
      $('#successRate').text(total === 0 ? '0%' : Math.round((committed / total) * 100) + '%');
      $('#unhealthyCount').text(unhealthy);
      $('#avgDuration').text(avgDuration + ' ms');
    }

    function statusBadge(status) {
      const classes = {
        Committed: 'bg-emerald-100 text-emerald-700',
        RolledBack: 'bg-amber-100 text-amber-700',
        Errored: 'bg-rose-100 text-rose-700'
      };

      return '<span class="rounded-full px-2.5 py-1 text-xs font-medium ' + (classes[status] || 'bg-slate-100 text-slate-700') + '">' + status + '</span>';
    }

    function renderTable() {
      const search = ($('#searchInput').val() || '').toString().toLowerCase().trim();
      const status = $('#statusFilter').val();
      const unhealthyOnly = $('#unhealthyOnly').is(':checked');

      const filtered = allTransactions.filter(item => {
        const method = (item.method || '').toLowerCase();
        const path = (item.path || '').toLowerCase();
        const matchesSearch = !search || method.includes(search) || path.includes(search);
        const matchesStatus = !status || item.status === status;
        const matchesUnhealthy = !unhealthyOnly || item.isUnhealthy;
        return matchesSearch && matchesStatus && matchesUnhealthy;
      });

      renderSummary(filtered);

      if (filtered.length === 0) {
        $('#rows').html('<tr><td colspan="7" class="px-4 py-6 text-center text-slate-500">No transactions found</td></tr>');
        return;
      }

      const markup = filtered.map(item => {
        return '<tr class="hover:bg-slate-50">' +
          '<td class="px-4 py-3 font-medium text-slate-900">' + (item.method || '-') + '</td>' +
          '<td class="px-4 py-3 text-slate-600">' + (item.path || '-') + '</td>' +
          '<td class="px-4 py-3">' + statusBadge(item.status) + '</td>' +
          '<td class="px-4 py-3 text-slate-700">' + item.durationMs + ' ms</td>' +
          '<td class="px-4 py-3 text-slate-700">' + item.connectionCount + '</td>' +
          '<td class="px-4 py-3 text-slate-700">' + item.executedQueryCount + '</td>' +
          '<td class="px-4 py-3 text-slate-600">' + formatDate(item.endedAt) + '</td>' +
        '</tr>';
      }).join('');

      $('#rows').html(markup);
    }

    function renderDistribution(data) {
      const keys = Object.keys(data || {});
      const max = keys.length === 0 ? 0 : Math.max(...keys.map(k => data[k]));

      if (keys.length === 0) {
        $('#distributionBars').html('<p class="text-sm text-slate-500">No distribution data</p>');
        return;
      }

      const bars = keys.map(key => {
        const value = data[key];
        const width = max === 0 ? 0 : Math.round((value / max) * 100);
        return '<div>' +
          '<div class="mb-1 flex items-center justify-between text-sm"><span class="font-medium text-slate-700">' + key + '</span><span class="text-slate-500">' + value + '</span></div>' +
          '<div class="h-2.5 rounded-full bg-slate-100"><div class="h-2.5 rounded-full bg-slate-800" style="width:' + width + '%"></div></div>' +
        '</div>';
      }).join('');

      $('#distributionBars').html(bars);
    }

    function loadTransactions() {
      const unhealthyOnly = $('#unhealthyOnly').is(':checked');
      const query = unhealthyOnly ? '?take=300&unhealthyOnly=true' : '?take=300';

      return $.getJSON('/tx-board/api/transactions' + query).done(function (data) {
        allTransactions = Array.isArray(data) ? data : [];
        renderTable();
      }).fail(function () {
        $('#rows').html('<tr><td colspan="7" class="px-4 py-6 text-center text-rose-600">Failed to load transactions</td></tr>');
      });
    }

    function loadDistribution() {
      return $.getJSON('/tx-board/api/distribution').done(function (data) {
        renderDistribution(data);
      }).fail(function () {
        $('#distributionBars').html('<p class="text-sm text-rose-600">Failed to load distribution</p>');
      });
    }

    function loadAll() {
      $.when(loadTransactions(), loadDistribution());
    }

    $(function () {
      $('#refreshBtn').on('click', loadAll);
      $('#refreshDistributionBtn').on('click', loadDistribution);
      $('#searchInput').on('input', renderTable);
      $('#statusFilter').on('change', renderTable);
      $('#unhealthyOnly').on('change', loadTransactions);
      $('#clearFiltersBtn').on('click', function () {
        $('#searchInput').val('');
        $('#statusFilter').val('');
        $('#unhealthyOnly').prop('checked', false);
        loadAll();
      });

      loadAll();
    });
  </script>
</body>
</html>
""";
}
