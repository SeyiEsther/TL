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
    function positionFloating(trigger, popover) {
        var rect = trigger.getBoundingClientRect();
        var gap = 6;
        var minW = popover.classList.contains('fs-popover-scroll') ? 300 : 280;
        var width = Math.min(Math.max(rect.width, minW), window.innerWidth - 24);
        var left = Math.max(12, Math.min(rect.left, window.innerWidth - width - 12));

        popover.style.width = width + 'px';
        popover.style.left = left + 'px';
        popover.style.visibility = 'hidden';
        popover.style.display = 'block';

        var popH = popover.offsetHeight;
        var spaceBelow = window.innerHeight - rect.bottom - gap;
        var spaceAbove = rect.top - gap;

        if (spaceBelow >= Math.min(popH, 200) || spaceBelow >= spaceAbove) {
            popover.style.top = (rect.bottom + gap) + 'px';
            popover.style.transformOrigin = 'top center';
        } else {
            popover.style.top = Math.max(12, rect.top - popH - gap) + 'px';
            popover.style.transformOrigin = 'bottom center';
        }
        popover.style.visibility = '';
    }

    function openPopover(wrapper, trigger, popover) {
        if (!wrapper._popoverHome) {
            wrapper._popoverHome = popover.parentNode;
            wrapper._popoverNext = popover.nextSibling;
        }
        document.body.appendChild(popover);
        wrapper._portaledPopover = popover;
        wrapper.classList.add('open');
        trigger.setAttribute('aria-expanded', 'true');
        popover.setAttribute('aria-hidden', 'false');
        requestAnimationFrame(function () {
            positionFloating(trigger, popover);
            popover.classList.add('is-open');
        });
    }

    function closePopover(wrapper, trigger, popover) {
        wrapper.classList.remove('open');
        trigger.setAttribute('aria-expanded', 'false');
        popover.classList.remove('is-open');
        popover.setAttribute('aria-hidden', 'true');

        var home = wrapper._popoverHome;
        if (home) {
            if (wrapper._popoverNext) home.insertBefore(popover, wrapper._popoverNext);
            else home.appendChild(popover);
        }
        popover.style.top = popover.style.left = popover.style.width = '';
        wrapper._portaledPopover = null;
    }

    function repositionOpen() {
        document.querySelectorAll('.dp.open, .fs.open').forEach(function (wrapper) {
            var trigger = wrapper.querySelector('.dp-trigger, .fs-trigger');
            var pop = wrapper._portaledPopover || wrapper.querySelector('.dp-popover, .fs-popover');
            if (trigger && pop && pop.classList.contains('is-open')) {
                positionFloating(trigger, pop);
            }
        });
    }

    function closeAllDropdowns(except) {
        document.querySelectorAll('.dp.open').forEach(function (dp) {
            if (dp !== except) closePicker(dp);
        });
        document.querySelectorAll('.fs.open').forEach(function (fs) {
            if (fs !== except) closeFilter(fs);
        });
    }
    function initSmoothNavigation() {
        function markNavigating() {
            document.documentElement.classList.add('is-navigating');
        }

        document.addEventListener('click', function (e) {
            var a = e.target.closest('a[href]');
            if (!a || a.target === '_blank' || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;
            if (a.classList.contains('pdf-dl') || a.hasAttribute('download')) return;
            var href = a.getAttribute('href');
            if (!href || href.charAt(0) === '#' || href.indexOf('javascript:') === 0) return;
            try {
                var url = new URL(a.href, location.href);
                if (url.origin !== location.origin) return;
            } catch (_) { return; }
            markNavigating();
        }, true);

        document.addEventListener('submit', function (e) {
            if (e.defaultPrevented) return;
            var form = e.target;
            if (form && form.getAttribute('data-no-nav-fade') !== null) return;
            markNavigating();
        }, true);
    }
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
    function initSdPanels() {
        var tabs = document.querySelectorAll('.sd-tab[data-tab]');
        var panels = document.querySelectorAll('.sd-panel');
        if (!tabs.length || !panels.length) return;

        tabs.forEach(function (tab) {
            tab.addEventListener('click', function () {
                var id = tab.dataset.tab;
                tabs.forEach(function (t) { t.classList.toggle('active', t === tab); });
                panels.forEach(function (p) {
                    p.classList.toggle('active', p.id === 'panel-' + id);
                });
            });
        });
    }
    function getPopover(dp) {
        return dp._portaledPopover || dp.querySelector('.dp-popover');
    }

    function closePicker(dp) {
        var pop = getPopover(dp);
        if (!pop) return;
        closePopover(dp, dp.querySelector('.dp-trigger'), pop);
        hideCalendar(dp);
    }

    function isCalendarVisible(dp) {
        var pop = getPopover(dp);
        if (!pop) return false;
        var cal = pop.querySelector('.dp-calendar');
        return cal && cal.getAttribute('aria-hidden') === 'false';
    }

    function repositionPicker(dp) {
        var pop = getPopover(dp);
        var trigger = dp.querySelector('.dp-trigger');
        if (pop && trigger && pop.classList.contains('is-open')) {
            requestAnimationFrame(function () { positionFloating(trigger, pop); });
        }
    }

    function hideCalendar(dp) {
        var pop = getPopover(dp);
        if (!pop) return;
        var cal = pop.querySelector('.dp-calendar');
        cal.setAttribute('aria-hidden', 'true');
        cal.style.display = 'none';
        pop.querySelector('.dp-quick').style.display = 'grid';
        pop.querySelector('.dp-custom-btn').style.display = 'flex';
        dp._calendarOpen = false;
        repositionPicker(dp);
    }

    function showCalendar(dp) {
        var pop = getPopover(dp);
        if (!pop) return;
        pop.querySelector('.dp-quick').style.display = 'none';
        pop.querySelector('.dp-custom-btn').style.display = 'none';
        var cal = pop.querySelector('.dp-calendar');
        cal.style.display = 'block';
        cal.setAttribute('aria-hidden', 'false');
        dp._calendarOpen = true;
        repositionPicker(dp);
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
        var pop = getPopover(dp);
        if (!pop) return;
        var val = getValue(dp);
        var today = new Date();
        today.setHours(0, 0, 0, 0);
        pop.querySelectorAll('.dp-quick-btn').forEach(function (btn) {
            var offset = +btn.dataset.offset;
            var target = addDays(today, offset);
            btn.classList.toggle('active', sameDay(val, target));
        });
    }

    function renderCalendar(dp, viewDate, draft) {
        var pop = getPopover(dp);
        if (!pop) return;
        var daysEl = pop.querySelector('.dp-days');
        var label = pop.querySelector('.dp-month-label');
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
                if (isCalendarVisible(dp)) {
                    hideCalendar(dp);
                } else {
                    closePicker(dp);
                }
            } else {
                closeAllDropdowns(dp);
                hideCalendar(dp);
                updateQuickActive(dp);
                openPopover(dp, trigger, pop);
            }
        });

        pop.addEventListener('click', function (e) {
            e.stopPropagation();
            var el = e.target.closest('.dp-quick-btn, .dp-custom-btn, .dp-back, .dp-cancel, .dp-save, .dp-nav, .dp-day:not(.dp-day-ghost)');
            if (!el) return;

            if (el.classList.contains('dp-quick-btn')) {
                var today = new Date();
                today.setHours(0, 0, 0, 0);
                setValue(dp, addDays(today, +el.dataset.offset));
                closePicker(dp);
                return;
            }
            if (el.classList.contains('dp-custom-btn')) {
                var val = getValue(dp) || new Date();
                showCalendar(dp);
                renderCalendar(dp, val, val);
                return;
            }
            if (el.classList.contains('dp-back') || el.classList.contains('dp-cancel')) {
                hideCalendar(dp);
                return;
            }
            if (el.classList.contains('dp-save')) {
                if (dp._draft) setValue(dp, dp._draft);
                closePicker(dp);
                return;
            }
            if (el.classList.contains('dp-nav')) {
                var base = dp._viewDate ? new Date(dp._viewDate) : new Date();
                base.setMonth(base.getMonth() + (+el.dataset.dir));
                renderCalendar(dp, base, dp._draft);
                repositionPicker(dp);
                return;
            }
            if (el.classList.contains('dp-day')) {
                pop.querySelectorAll('.dp-day.selected').forEach(function (node) { node.classList.remove('selected'); });
                el.classList.add('selected');
                var y = dp._viewDate.getFullYear();
                var m = dp._viewDate.getMonth();
                dp._draft = new Date(y, m, +el.textContent);
            }
        });
    }
    function getFilterPopover(fs) {
        return fs._portaledPopover || fs.querySelector('.fs-popover');
    }

    function setFilterValue(fs, value, label, placeholder) {
        var input = fs.querySelector('.fs-input');
        var display = fs.querySelector('.fs-display');
        input.value = value;
        display.textContent = value ? label : placeholder;
        display.classList.toggle('fs-placeholder', !value);
        fs.querySelectorAll('.fs-quick-btn, .fs-option').forEach(function (btn) {
            btn.classList.toggle('active', btn.dataset.value === value);
        });
    }

    function closeFilter(fs) {
        var pop = getFilterPopover(fs);
        if (!pop) return;
        closePopover(fs, fs.querySelector('.fs-trigger'), pop);
        var search = pop.querySelector('.fs-search');
        if (search) { search.value = ''; filterAreaOptions(fs, ''); }
    }

    function filterAreaOptions(fs, query) {
        var pop = getFilterPopover(fs);
        if (!pop) return;
        var q = query.toLowerCase().trim();
        pop.querySelectorAll('.fs-group').forEach(function (group) {
            var anyVisible = false;
            group.querySelectorAll('.fs-option').forEach(function (opt) {
                var text = opt.dataset.search || opt.textContent.toLowerCase();
                var show = !q || text.indexOf(q) >= 0;
                opt.classList.toggle('hidden', !show);
                if (show) anyVisible = true;
            });
            group.classList.toggle('hidden', !anyVisible);
        });
    }

    function initFilter(fs) {
        var trigger = fs.querySelector('.fs-trigger');
        var pop = fs.querySelector('.fs-popover');
        var placeholder = fs.dataset.placeholder || 'Select…';

        trigger.addEventListener('click', function (e) {
            e.stopPropagation();
            if (fs.classList.contains('open')) {
                closeFilter(fs);
            } else {
                closeAllDropdowns(fs);
                openPopover(fs, trigger, pop);
                var search = fs.querySelector('.fs-search');
                if (search) {
                    setTimeout(function () { search.focus(); }, 80);
                }
            }
        });

        pop.addEventListener('click', function (e) { e.stopPropagation(); });

        fs.querySelectorAll('.fs-quick-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var val = btn.dataset.value;
                var label = btn.querySelector('span').textContent;
                setFilterValue(fs, val, label, placeholder);
                closeFilter(fs);
            });
        });

        fs.querySelectorAll('.fs-option').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var val = btn.dataset.value;
                var label = btn.querySelector('.fs-option-label').textContent;
                setFilterValue(fs, val, val ? label : placeholder, placeholder);
                closeFilter(fs);
            });
        });

        var search = fs.querySelector('.fs-search');
        if (search) {
            search.addEventListener('input', function () {
                filterAreaOptions(fs, search.value);
            });
            search.addEventListener('click', function (e) { e.stopPropagation(); });
        }
    }

    document.addEventListener('click', function () { closeAllDropdowns(null); });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') closeAllDropdowns(null);
    });
    function isIOSDevice() {
        return /iPad|iPhone|iPod/.test(navigator.userAgent) ||
            (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
    }

    function initPdfDownloads() {
        document.addEventListener('click', function (e) {
            var a = e.target.closest('a[href*="/pdf"]');
            if (!a) return;

            var url = a.getAttribute('href');
            if (!url) return;

            e.preventDefault();

            var name = a.getAttribute('data-filename') || 'Rittal_Report.pdf';
            a.classList.add('pdf-loading');
            a.setAttribute('aria-busy', 'true');

            fetch(url, {
                credentials: 'same-origin',
                headers: { Accept: 'application/pdf, application/octet-stream' }
            })
                .then(function (res) {
                    var ct = (res.headers.get('content-type') || '').toLowerCase();
                    if (!res.ok) {
                        throw new Error('Could not load PDF (server returned ' + res.status + ').');
                    }
                    if (ct.indexOf('pdf') === -1 && ct.indexOf('octet-stream') === -1) {
                        throw new Error(
                            'Expected a PDF but got ' + (ct || 'a web page') + ' instead. ' +
                            'On iPad this usually means the server needs anonymous access to /api — contact IT.'
                        );
                    }
                    return res.blob();
                })
                .then(function (blob) {
                    var pdfBlob = blob.type && blob.type.indexOf('pdf') >= 0
                        ? blob
                        : new Blob([blob], { type: 'application/pdf' });
                    var objUrl = URL.createObjectURL(pdfBlob);

                    if (isIOSDevice()) {
                        var win = window.open(objUrl, '_blank');
                        if (!win) window.location.assign(objUrl);
                    } else {
                        var link = document.createElement('a');
                        link.href = objUrl;
                        link.download = name;
                        link.rel = 'noopener';
                        document.body.appendChild(link);
                        link.click();
                        link.remove();
                    }

                    setTimeout(function () { URL.revokeObjectURL(objUrl); }, 120000);
                })
                .catch(function (err) {
                    alert(err.message || 'PDF download failed.');
                })
                .finally(function () {
                    a.classList.remove('pdf-loading');
                    a.removeAttribute('aria-busy');
                });
        });
    }
    function initAuditForms() {
        document.querySelectorAll('[data-audit-form]').forEach(function (wrap) {
            var form = wrap.querySelector('form');
            if (!form) return;

            var fill = wrap.querySelector('.audit-progress-fill');
            var text = wrap.querySelector('.audit-progress-text');
            var links = wrap.querySelectorAll('.audit-sec-link');
            var sections = wrap.querySelectorAll('.audit-card[id]');

            function countProgress() {
                var groups = {};
                form.querySelectorAll('.audit-yn input[type="radio"], .audit-binary input[type="radio"], .score-3 input[type="radio"]').forEach(function (r) {
                    if (!groups[r.name]) groups[r.name] = false;
                    if (r.checked) groups[r.name] = true;
                });
                form.querySelectorAll('.status-pills input[type="radio"]:checked, .verdict-pills input[type="radio"]:checked').forEach(function (r) {
                    if (r.value) groups[r.name] = true;
                });
                var names = Object.keys(groups);
                var done = names.filter(function (n) { return groups[n]; }).length;
                var pct = names.length ? Math.round((done / names.length) * 100) : 0;
                if (fill) fill.style.width = pct + '%';
                if (text) text.textContent = done + '/' + names.length;
            }

            function updateActiveSection() {
                if (!sections.length || !links.length) return;
                var offset = 140;
                var current = sections[0];
                sections.forEach(function (sec) {
                    if (sec.getBoundingClientRect().top - offset <= 0) current = sec;
                });
                links.forEach(function (link) {
                    var href = link.getAttribute('href') || '';
                    link.classList.toggle('active', href === '#' + current.id);
                });
            }

            form.addEventListener('change', countProgress);
            countProgress();

            links.forEach(function (link) {
                link.addEventListener('click', function (e) {
                    var id = link.getAttribute('href');
                    if (!id || id.charAt(0) !== '#') return;
                    var target = wrap.querySelector(id);
                    if (!target) return;
                    e.preventDefault();
                    target.scrollIntoView({ behavior: reducedMotion ? 'auto' : 'smooth', block: 'start' });
                });
            });

            window.addEventListener('scroll', updateActiveSection, { passive: true });
            updateActiveSection();
        });
    }

    function initShiftHistoryHub() {
        var hub = document.querySelector('[data-shift-hub]');
        if (!hub) return;

        var cards = Array.prototype.slice.call(hub.querySelectorAll('[data-hub-card]'));
        var tabs = Array.prototype.slice.call(hub.querySelectorAll('[data-hub-tab]'));
        var dots = Array.prototype.slice.call(hub.querySelectorAll('[data-hub-dot]'));
        var panels = Array.prototype.slice.call(hub.querySelectorAll('[data-hub-panel]'));
        if (!cards.length) return;

        var order = cards.map(function (c) { return c.getAttribute('data-hub-card'); });
        var idx = Math.max(0, order.indexOf(hub.getAttribute('data-default') || order[0]));
        var expanded = null;
        var rotateTimer = null;
        var paused = false;

        function showCard(id) {
            var i = order.indexOf(id);
            if (i < 0) return;
            idx = i;
            cards.forEach(function (card) {
                var on = card.getAttribute('data-hub-card') === id;
                card.classList.toggle('is-active', on);
                card.classList.remove('is-prev');
            });
            tabs.forEach(function (tab) {
                var on = tab.getAttribute('data-hub-tab') === id;
                tab.classList.toggle('active', on);
                tab.setAttribute('aria-selected', on ? 'true' : 'false');
            });
            dots.forEach(function (dot) {
                dot.classList.toggle('active', dot.getAttribute('data-hub-dot') === id);
            });
        }

        function openPanel(panel) {
            panel.removeAttribute('hidden');
            if (reducedMotion) {
                panel.classList.add('is-open');
                return;
            }
            requestAnimationFrame(function () {
                requestAnimationFrame(function () {
                    panel.classList.add('is-open');
                });
            });
        }

        function closePanel(panel) {
            panel.classList.remove('is-open');
            if (reducedMotion) {
                panel.setAttribute('hidden', '');
                return;
            }
            var done = function (e) {
                if (e.propertyName !== 'grid-template-rows') return;
                panel.setAttribute('hidden', '');
                panel.removeEventListener('transitionend', done);
            };
            panel.addEventListener('transitionend', done);
        }

        function collapseAll() {
            expanded = null;
            panels.forEach(closePanel);
            paused = false;
            startRotate();
        }

        function expandPanel(id) {
            pauseRotate();
            expanded = id;
            panels.forEach(function (p) {
                if (p.getAttribute('data-hub-panel') === id) openPanel(p);
                else closePanel(p);
            });
            showCard(id);
            var panel = hub.querySelector('[data-hub-panel="' + id + '"]');
            if (panel && !reducedMotion) {
                requestAnimationFrame(function () {
                    panel.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
                });
            }
        }

        function nextCard() {
            if (expanded || paused) return;
            showCard(order[(idx + 1) % order.length]);
        }

        function startRotate() {
            stopRotate();
            if (order.length < 2 || reducedMotion) return;
            rotateTimer = setInterval(nextCard, 7000);
        }

        function stopRotate() {
            if (rotateTimer) clearInterval(rotateTimer);
            rotateTimer = null;
        }

        function pauseRotate() {
            paused = true;
            stopRotate();
        }

        tabs.forEach(function (tab) {
            tab.addEventListener('click', function () {
                pauseRotate();
                showCard(tab.getAttribute('data-hub-tab'));
                if (!expanded) startRotate();
            });
        });

        cards.forEach(function (card) {
            card.addEventListener('click', function (e) {
                if (e.target.closest('.shift-preview-expand')) return;
                expandPanel(card.getAttribute('data-hub-card'));
            });
        });

        hub.querySelectorAll('[data-hub-expand]').forEach(function (btn) {
            btn.addEventListener('click', function (e) {
                e.stopPropagation();
                expandPanel(btn.getAttribute('data-hub-expand'));
            });
        });

        hub.querySelectorAll('[data-hub-collapse]').forEach(function (btn) {
            btn.addEventListener('click', collapseAll);
        });

        hub.addEventListener('mouseenter', pauseRotate);
        hub.addEventListener('mouseleave', function () {
            paused = false;
            if (!expanded) startRotate();
        });

        showCard(order[idx]);
        startRotate();
    }

    window.addEventListener('resize', repositionOpen);
    window.addEventListener('scroll', repositionOpen, true);

    document.addEventListener('DOMContentLoaded', function () {
        initSmoothNavigation();
        initTabBars();
        initSdPanels();
        initPdfDownloads();
        initAuditForms();
        initShiftHistoryHub();
        document.querySelectorAll('.dp').forEach(initPicker);
        document.querySelectorAll('.fs').forEach(initFilter);
    });
})();
