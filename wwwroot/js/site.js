(function () {
    'use strict';

    var MONTHS = ['January', 'February', 'March', 'April', 'May', 'June',
        'July', 'August', 'September', 'October', 'November', 'December'];

    var reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    function pad(n) { return String(n).padStart(2, '0'); }

    function toIso(d) {
        return d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate());
    }

    function parseIso(str) {
        if (!str) return null;
        var p = str.split('-');
        if (p.length !== 3) return null;
        var d = new Date(+p[0], +p[1] - 1, +p[2]);
        return isNaN(d.getTime()) ? null : d;
    }

    function formatDisplay(d) {
        return d.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });
    }

    function addDays(d, n) {
        var r = new Date(d);
        r.setDate(r.getDate() + n);
        return r;
    }

    function sameDay(a, b) {
        return a && b && a.getFullYear() === b.getFullYear()
            && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
    }

    /* ── Staggered page reveals ── */
    function initReveals() {
        if (reducedMotion) return;
        var sel = '.card, .kpi, .page-head, .welcome-card, .success-card, .chart-card, .session-bar, .grid-wrap, .audit-card, .kpi-row, .chart-grid-4, .chart-grid-act, .summary-grid, .submit-bar, .shift-once-card';
        var els = document.querySelectorAll(sel);
        els.forEach(function (el, i) {
            el.classList.add('reveal');
            el.style.transitionDelay = (i % 10) * 0.05 + 's';
        });
        requestAnimationFrame(function () {
            requestAnimationFrame(function () {
                els.forEach(function (el) { el.classList.add('visible'); });
            });
        });
    }

    /* ── Tab bar sliding indicator ── */
    function initTabBars() {
        document.querySelectorAll('.sd-tab-bar').forEach(function (bar) {
            var tabs = bar.querySelectorAll('.sd-tab');
            if (!tabs.length) return;

            var indicator = document.createElement('span');
            indicator.className = 'sd-tab-indicator';
            bar.insertBefore(indicator, bar.firstChild);

            function moveTo(tab) {
                if (!tab) return;
                var barRect = bar.getBoundingClientRect();
                var tabRect = tab.getBoundingClientRect();
                indicator.style.left = (tabRect.left - barRect.left + bar.scrollLeft) + 'px';
                indicator.style.width = tabRect.width + 'px';
            }

            var active = bar.querySelector('.sd-tab.active') || tabs[0];
            if (active) moveTo(active);

            tabs.forEach(function (tab) {
                tab.addEventListener('click', function () {
                    requestAnimationFrame(function () { moveTo(tab); });
                });
            });

            window.addEventListener('resize', function () {
                moveTo(bar.querySelector('.sd-tab.active'));
            });
        });
    }

    /* ── Senior dashboard panels ── */
    function initSdPanels() {
        var tabs = document.querySelectorAll('.sd-tab[data-tab]');
        var panels = document.querySelectorAll('.sd-panel');
        if (!tabs.length || !panels.length) return;

        tabs.forEach(function (tab) {
            tab.addEventListener('click', function () {
                var id = tab.dataset.tab;
                tabs.forEach(function (t) { t.classList.toggle('active', t === tab); });
                panels.forEach(function (p) {
                    var on = p.id === 'panel-' + id;
                    p.classList.toggle('active', on);
                });
            });
        });
    }

    /* ── Date picker ── */
    function closeAll(except) {
        document.querySelectorAll('.dp.open').forEach(function (dp) {
            if (dp !== except) closePicker(dp);
        });
    }

    function closePicker(dp) {
        dp.classList.remove('open');
        dp.querySelector('.dp-trigger').setAttribute('aria-expanded', 'false');
        dp.querySelector('.dp-popover').setAttribute('aria-hidden', 'true');
        hideCalendar(dp);
    }

    function hideCalendar(dp) {
        var cal = dp.querySelector('.dp-calendar');
        cal.setAttribute('aria-hidden', 'true');
        cal.style.display = 'none';
        dp.querySelector('.dp-quick').style.display = '';
        dp.querySelector('.dp-custom-btn').style.display = '';
    }

    function showCalendar(dp) {
        dp.querySelector('.dp-quick').style.display = 'none';
        dp.querySelector('.dp-custom-btn').style.display = 'none';
        var cal = dp.querySelector('.dp-calendar');
        cal.style.display = '';
        cal.setAttribute('aria-hidden', 'false');
    }

    function getValue(dp) {
        return parseIso(dp.querySelector('.dp-input').value);
    }

    function setValue(dp, date) {
        var input = dp.querySelector('.dp-input');
        var display = dp.querySelector('.dp-display');
        if (date) {
            input.value = toIso(date);
            display.textContent = formatDisplay(date);
            display.classList.remove('dp-placeholder');
        } else {
            input.value = '';
            display.textContent = 'Select date';
            display.classList.add('dp-placeholder');
        }
        updateQuickActive(dp);
    }

    function updateQuickActive(dp) {
        var val = getValue(dp);
        var today = new Date();
        today.setHours(0, 0, 0, 0);
        dp.querySelectorAll('.dp-quick-btn').forEach(function (btn) {
            var offset = +btn.dataset.offset;
            var target = addDays(today, offset);
            btn.classList.toggle('active', sameDay(val, target));
        });
    }

    function renderCalendar(dp, viewDate, draft) {
        var daysEl = dp.querySelector('.dp-days');
        var label = dp.querySelector('.dp-month-label');
        var y = viewDate.getFullYear();
        var m = viewDate.getMonth();
        label.textContent = MONTHS[m] + ' ' + y;

        var first = new Date(y, m, 1);
        var startDow = (first.getDay() + 6) % 7;
        var daysInMonth = new Date(y, m + 1, 0).getDate();
        var prevDays = new Date(y, m, 0).getDate();

        daysEl.innerHTML = '';
        for (var i = 0; i < startDow; i++) {
            var ghost = document.createElement('button');
            ghost.type = 'button';
            ghost.className = 'dp-day dp-day-ghost';
            ghost.textContent = prevDays - startDow + i + 1;
            ghost.disabled = true;
            daysEl.appendChild(ghost);
        }
        for (var d = 1; d <= daysInMonth; d++) {
            (function (dayNum) {
                var btn = document.createElement('button');
                btn.type = 'button';
                btn.className = 'dp-day';
                btn.textContent = dayNum;
                var cellDate = new Date(y, m, dayNum);
                if (draft && sameDay(cellDate, draft)) btn.classList.add('selected');
                btn.addEventListener('click', function () {
                    dp.querySelectorAll('.dp-day.selected').forEach(function (el) { el.classList.remove('selected'); });
                    btn.classList.add('selected');
                    dp._draft = new Date(y, m, dayNum);
                });
                daysEl.appendChild(btn);
            })(d);
        }
        dp._viewDate = new Date(y, m, 1);
        dp._draft = draft;
    }

    function initPicker(dp) {
        var trigger = dp.querySelector('.dp-trigger');
        var pop = dp.querySelector('.dp-popover');

        if (!getValue(dp)) {
            dp.querySelector('.dp-display').classList.add('dp-placeholder');
        }
        updateQuickActive(dp);
        hideCalendar(dp);

        trigger.addEventListener('click', function (e) {
            e.stopPropagation();
            if (dp.classList.contains('open')) {
                closePicker(dp);
            } else {
                closeAll(dp);
                dp.classList.add('open');
                trigger.setAttribute('aria-expanded', 'true');
                pop.setAttribute('aria-hidden', 'false');
                hideCalendar(dp);
                updateQuickActive(dp);
            }
        });

        pop.addEventListener('click', function (e) { e.stopPropagation(); });

        dp.querySelectorAll('.dp-quick-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var today = new Date();
                today.setHours(0, 0, 0, 0);
                setValue(dp, addDays(today, +btn.dataset.offset));
                closePicker(dp);
            });
        });

        dp.querySelector('.dp-custom-btn').addEventListener('click', function () {
            var val = getValue(dp) || new Date();
            showCalendar(dp);
            renderCalendar(dp, val, val);
        });

        dp.querySelectorAll('.dp-nav').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var vd = dp._viewDate || new Date();
                vd.setMonth(vd.getMonth() + (+btn.dataset.dir));
                renderCalendar(dp, vd, dp._draft);
            });
        });

        dp.querySelector('.dp-cancel').addEventListener('click', function () {
            hideCalendar(dp);
        });

        dp.querySelector('.dp-save').addEventListener('click', function () {
            if (dp._draft) setValue(dp, dp._draft);
            closePicker(dp);
        });
    }

    document.addEventListener('click', function () { closeAll(null); });

    document.addEventListener('DOMContentLoaded', function () {
        initReveals();
        initTabBars();
        initSdPanels();
        document.querySelectorAll('.dp').forEach(initPicker);
    });
})();
