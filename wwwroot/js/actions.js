/* Structured actions (PDCA) — client helpers. Completing an action updates the
   nav badge immediately from the count the server returns, no page refresh. */
(function () {
    'use strict';

    function updateBadge(n) {
        var b = document.getElementById('nav-action-badge');
        if (!b) return;
        if (n && n > 0) { b.textContent = n; b.style.display = ''; }
        else { b.style.display = 'none'; }
    }

    async function postJson(url, body) {
        var res = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: body ? JSON.stringify(body) : null
        });
        var data = null;
        try { data = await res.json(); } catch (e) { }
        return { ok: res.ok, data: data || {} };
    }

    window.Actions = {
        // noteEl: the note input for this row; rowEl: the row to remove on success.
        complete: async function (id, noteEl, rowEl) {
            var note = (noteEl && noteEl.value || '').trim();
            if (note.length < 3) {
                alert('Please describe what you actually did to complete this action.');
                if (noteEl) noteEl.focus();
                return;
            }
            var r = await postJson('/api/actions/' + id + '/complete', { note: note });
            if (!r.ok) { alert(r.data.error || 'Could not complete this action.'); return; }
            if (rowEl && rowEl.remove) rowEl.remove();
            updateBadge(r.data.openCount);
        },
        reassign: async function (id, selEl) {
            var owner = selEl && selEl.value;
            if (!owner) { alert('Choose an owner to reassign to.'); return; }
            var r = await postJson('/api/actions/' + id + '/reassign', { owner: owner });
            if (!r.ok) { alert(r.data.error || 'Could not reassign.'); return; }
            location.reload();
        },
        reopen: async function (id) {
            if (!confirm('Reopen this completed action? It returns to the open list.')) return;
            var r = await postJson('/api/actions/' + id + '/reopen', null);
            if (!r.ok) { alert(r.data.error || 'Could not reopen.'); return; }
            location.reload();
        }
    };
})();
