(function () {
    'use strict';

    const MONTHS = ['January', 'February', 'March', 'April', 'May', 'June',
        'July', 'August', 'September', 'October', 'November', 'December'];

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

    function closeAll(except) {
        document.querySelectorAll('.dp.open').forEach(function (dp) {
            if (dp !== except) closePicker(dp);
        });
    }

    function closePicker(dp) {
        dp.classList.remove('open');
        dp.querySelector('.dp-trigger').setAttribute('aria-expanded', 'false');
        var pop = dp.querySelector('.dp-popover');
        pop.hidden = true;
        hideCalendar(dp);
    }

    function hideCalendar(dp) {
        var cal = dp.querySelector('.dp-calendar');
        cal.hidden = true;
        dp.querySelector('.dp-quick').hidden = false;
        dp.querySelector('.dp-custom-btn').hidden = false;
    }

    function showCalendar(dp) {
        dp.querySelector('.dp-quick').hidden = true;
        dp.querySelector('.dp-custom-btn').hidden = true;
        dp.querySelector('.dp-calendar').hidden = false;
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
            var btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'dp-day';
            btn.textContent = d;
            var cellDate = new Date(y, m, d);
            if (draft && sameDay(cellDate, draft)) btn.classList.add('selected');
            btn.addEventListener('click', function (ev) {
                dp.querySelectorAll('.dp-day.selected').forEach(function (el) { el.classList.remove('selected'); });
                ev.currentTarget.classList.add('selected');
                dp._draft = new Date(y, m, +ev.currentTarget.textContent);
            });
            daysEl.appendChild(btn);
        }
        dp._viewDate = new Date(y, m, 1);
        dp._draft = draft;
    }

    function initPicker(dp) {
        var trigger = dp.querySelector('.dp-trigger');
        var pop = dp.querySelector('.dp-popover');
        var cal = dp.querySelector('.dp-calendar');

        if (!getValue(dp)) {
            dp.querySelector('.dp-display').classList.add('dp-placeholder');
        }
        updateQuickActive(dp);

        trigger.addEventListener('click', function (e) {
            e.stopPropagation();
            if (dp.classList.contains('open')) {
                closePicker(dp);
            } else {
                closeAll(dp);
                dp.classList.add('open');
                trigger.setAttribute('aria-expanded', 'true');
                pop.hidden = false;
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
        document.querySelectorAll('.dp').forEach(initPicker);
    });
})();
