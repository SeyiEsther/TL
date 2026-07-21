/*
 * form-backup.js — app-wide safety net against lost form input.
 *
 * Any <form data-backup="name"> (optionally data-backup-key="id") is mirrored
 * to the browser's localStorage as the user types. This sits ON TOP OF server
 * autosave: if a server save is ever rejected (e.g. a stale antiforgery token
 * after an app restart — the bug that lost a HoD audit), the answers are still
 * held locally and restored when the page reloads.
 *
 * It reconciles against the server on load rather than clearing blindly:
 *   - fields the server already has          -> nothing to do
 *   - fields the server is missing but we have -> restored + a "restored" banner
 *   - server already has EVERYTHING            -> the backup clears itself
 * So it never clobbers good server data and never needs submit/success hooks.
 */
(function () {
    'use strict';

    var PREFIX = 'tlfb:';
    var MAX_AGE_MS = 7 * 24 * 60 * 60 * 1000; // 7 days
    var SKIP_NAMES = { '__RequestVerificationToken': true };

    function safeGet(k) { try { return localStorage.getItem(k); } catch (e) { return null; } }
    function safeSet(k, v) { try { localStorage.setItem(k, v); } catch (e) { /* private mode / quota */ } }
    function safeRemove(k) { try { localStorage.removeItem(k); } catch (e) { } }

    function keyFor(form) {
        var name = form.getAttribute('data-backup') || 'form';
        var id = form.getAttribute('data-backup-key') || '';
        return PREFIX + location.pathname + '::' + name + (id ? '::' + id : '');
    }

    function backupFields(form) {
        return Array.prototype.filter.call(
            form.querySelectorAll('input, textarea, select'),
            function (el) {
                if (!el.name || SKIP_NAMES[el.name]) return false;
                if (el.disabled) return false;
                if (el.hasAttribute('data-no-backup')) return false;
                var t = (el.type || '').toLowerCase();
                return t !== 'hidden' && t !== 'password' && t !== 'file' &&
                       t !== 'submit' && t !== 'button' && t !== 'reset';
            });
    }

    // Snapshot the form's current values into a plain object.
    function snapshot(form) {
        var data = {};
        backupFields(form).forEach(function (el) {
            var t = (el.type || '').toLowerCase();
            if (t === 'radio' || t === 'checkbox') {
                if (el.checked) {
                    (data[el.name] = data[el.name] || []).push(el.value);
                }
            } else {
                if (el.value !== '') data[el.name] = el.value;
            }
        });
        return data;
    }

    function isEmpty(obj) { for (var k in obj) if (obj.hasOwnProperty(k)) return false; return true; }

    function save(form) {
        var data = snapshot(form);
        if (isEmpty(data)) { safeRemove(keyFor(form)); return; }
        safeSet(keyFor(form), JSON.stringify({ t: Date.now(), d: data }));
    }

    // Restore only values the form is currently MISSING. Returns count restored.
    function reconcile(form) {
        var raw = safeGet(keyFor(form));
        if (!raw) return 0;

        var parsed;
        try { parsed = JSON.parse(raw); } catch (e) { safeRemove(keyFor(form)); return 0; }
        if (!parsed || !parsed.d || (Date.now() - (parsed.t || 0)) > MAX_AGE_MS) {
            safeRemove(keyFor(form));
            return 0;
        }

        var data = parsed.d;
        var restored = 0;

        Object.keys(data).forEach(function (name) {
            var value = data[name];
            // Names like A[0].Pass contain [ ] . — safe inside a quoted attribute
            // selector once " and \ are escaped; do NOT use CSS.escape here.
            var sel = '[name="' + name.replace(/\\/g, '\\\\').replace(/"/g, '\\"') + '"]';
            var els = form.querySelectorAll(sel);
            if (!els.length) return;
            var first = els[0];
            var t = (first.type || '').toLowerCase();

            if (t === 'radio' || t === 'checkbox') {
                var wanted = Array.isArray(value) ? value : [value];
                // only restore if nothing in this group is currently chosen
                var anyChecked = Array.prototype.some.call(els, function (e) { return e.checked; });
                if (anyChecked) return;
                var did = false;
                Array.prototype.forEach.call(els, function (e) {
                    if (wanted.indexOf(e.value) !== -1) {
                        e.checked = true;
                        e.dispatchEvent(new Event('change', { bubbles: true }));
                        did = true;
                    }
                });
                if (did) restored++;
            } else {
                if (first.value !== '') return; // server already has a value — keep it
                first.value = Array.isArray(value) ? value[0] : value;
                first.dispatchEvent(new Event('input', { bubbles: true }));
                first.dispatchEvent(new Event('change', { bubbles: true }));
                restored++;
            }
        });

        // If the server already had everything (nothing needed restoring), the
        // backup is redundant — drop it so it doesn't linger or nag.
        if (restored === 0) safeRemove(keyFor(form));
        return restored;
    }

    function showBanner(form, count) {
        if (document.querySelector('.tlfb-banner')) return;
        var bar = document.createElement('div');
        bar.className = 'tlfb-banner';
        bar.setAttribute('role', 'status');
        bar.style.cssText = 'position:fixed;left:50%;transform:translateX(-50%);bottom:18px;z-index:9999;' +
            'background:#0f172a;color:#e2e8f0;padding:10px 14px;border-radius:8px;font-size:13px;' +
            'box-shadow:0 6px 20px rgba(0,0,0,.3);display:flex;align-items:center;gap:12px;max-width:92vw';
        var msg = document.createElement('span');
        msg.textContent = 'Restored ' + count + ' unsaved change' + (count === 1 ? '' : 's') +
            ' from this browser. Check them, then save/submit.';
        var btn = document.createElement('button');
        btn.type = 'button';
        btn.textContent = 'Dismiss';
        btn.style.cssText = 'background:#334155;color:#fff;border:0;border-radius:6px;padding:5px 10px;cursor:pointer;font-size:12px';
        btn.addEventListener('click', function () { bar.remove(); });
        bar.appendChild(msg); bar.appendChild(btn);
        document.body.appendChild(bar);
        setTimeout(function () { if (bar.parentNode) bar.remove(); }, 12000);
    }

    function init(form) {
        var restored = reconcile(form);
        if (restored > 0) showBanner(form, restored);

        var timer = null;
        function schedule() {
            clearTimeout(timer);
            timer = setTimeout(function () { save(form); }, 400);
        }
        form.addEventListener('input', schedule);
        form.addEventListener('change', schedule);
        // Flush immediately if the page is being hidden/unloaded.
        window.addEventListener('pagehide', function () { save(form); });
        document.addEventListener('visibilitychange', function () {
            if (document.visibilityState === 'hidden') save(form);
        });
    }

    // Public hook: server autosave can call this on a confirmed save to trim the
    // local backup early (optional — reconcile-on-load already handles it).
    window.TLFormBackup = {
        clear: function (formOrKey) {
            if (typeof formOrKey === 'string') { safeRemove(formOrKey); return; }
            if (formOrKey && formOrKey.getAttribute) safeRemove(keyFor(formOrKey));
        },
        save: save
    };

    function boot() {
        document.querySelectorAll('form[data-backup]').forEach(init);
    }
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', boot);
    } else {
        boot();
    }
})();
