/// <reference path="~/lib/jquery/dist/jquery.min.js" />

(function () {
    "use strict";

    if (!window.queueLinkConfig) return;

    var cfg = window.queueLinkConfig;
    var isCustomer = cfg.isCustomer;
    var isStaff = cfg.isStaff;
    var queueServiceId = cfg.queueServiceId;
    var publicToken = cfg.publicToken;
    var notifyWhenAheadAtMost = typeof cfg.notifyWhenAheadAtMost === "number"
        ? cfg.notifyWhenAheadAtMost
        : 3;

    // ── State ──────────────────────────────────────────────────────
    var lastStatus = null;
    var lastPeopleAhead = null;
    var approachingNotified = false;

    // ── SignalR connection ─────────────────────────────────────────
    var connection = new signalR.HubConnectionBuilder()
        .withUrl("/queueHub")
        .withAutomaticReconnect()
        .build();

    if (isCustomer && publicToken) {
        connection.start()
            .then(function () { return connection.invoke("JoinTicketGroup", publicToken); })
            .catch(function (err) { console.warn("SignalR connect failed:", err); });
    }

    if (isStaff && queueServiceId) {
        connection.start()
            .then(function () { return connection.invoke("JoinQueueGroup", queueServiceId); })
            .catch(function (err) { console.warn("SignalR connect failed:", err); });
    }

    // ── Events ─────────────────────────────────────────────────────
    connection.on("TicketUpdated", function (data) {
        if (isCustomer && data.publicToken === publicToken) {
            applyTicketUpdate(data);
        }
        if (isStaff && data.queueServiceId === queueServiceId) {
            location.reload();
        }
    });

    connection.on("QueueUpdated", function (data) {
        if (isStaff && data.queueServiceId === queueServiceId) {
            location.reload();
        }
        if (isCustomer && data.queueServiceId === queueServiceId) {
            refreshFromServer();
        }
    });

    connection.on("CurrentlyCallingChanged", function (data) {
        if (isCustomer && data.queueServiceId === queueServiceId) {
            if (data.publicToken === publicToken) {
                // Đến lượt mình rồi — reload để hiển thị trạng thái Called.
                location.reload();
            } else {
                showToast("Đang gọi số: " + data.ticketCode, "warning");
            }
        }
        if (isStaff && data.queueServiceId === queueServiceId) {
            var el = document.getElementById("currentCall");
            if (el) el.textContent = data.ticketCode;
        }
    });

    // ── DOM update (no full reload) ────────────────────────────────
    function applyTicketUpdate(data) {
        if (!data) return;
        if (data.status && data.status !== lastStatus) {
            // Trạng thái thay đổi → reload để render layout chuẩn (badge class, alert class, message).
            location.reload();
            return;
        }
        // Status không đổi nhưng ticket đã được "advance" → gọi server lấy số liệu mới.
        refreshFromServer();
    }

    function refreshFromServer() {
        if (!publicToken) return;
        fetch("/Queue/GetTicketStatus?token=" + encodeURIComponent(publicToken))
            .then(function (r) { return r.ok ? r.json() : null; })
            .then(function (data) {
                if (!data) return;
                updateStatusBadge(data.statusText);
                updatePeopleAhead(data.peopleAhead);
                updateEstimatedWait(data.estimatedWaitMinutes);
                updateCurrentlyCalling(data.currentCallingTicketCode);
                checkApproaching(data.peopleAhead);
            })
            .catch(function () { });
    }

    function updateStatusBadge(text) {
        if (lastStatus === text) return;
        lastStatus = text;
        var el = document.getElementById("statusText");
        if (el) el.textContent = text;
    }

    function updatePeopleAhead(value) {
        if (lastPeopleAhead === value) return;
        lastPeopleAhead = value;
        var el = document.getElementById("peopleAhead");
        if (el) el.textContent = value + " người";
    }

    function updateEstimatedWait(minutes) {
        var el = document.getElementById("estimatedWait");
        if (el) el.textContent = "~" + minutes + " phút";
    }

    function updateCurrentlyCalling(code) {
        var el = document.getElementById("currentCallCode");
        if (!el) return;
        if (code) {
            el.textContent = code;
            el.closest(".alert")?.classList.remove("d-none");
        } else {
            el.closest(".alert")?.classList.add("d-none");
        }
    }

    function checkApproaching(peopleAhead) {
        if (peopleAhead == null) return;
        if (peopleAhead > 0 && peopleAhead <= notifyWhenAheadAtMost && !approachingNotified) {
            approachingNotified = true;
            var msg = "Sắp đến lượt! Còn " + peopleAhead + " người trước bạn.";
            showToast(msg, "warning");
            showBrowserNotification("Sắp đến lượt", msg);
        }
        if (peopleAhead > notifyWhenAheadAtMost) {
            approachingNotified = false;
        }
    }

    // ── Toasts ─────────────────────────────────────────────────────
    function showToast(message, type) {
        var toast = document.createElement("div");
        toast.className = "position-fixed top-0 end-0 p-3";
        toast.style.zIndex = "9999";
        toast.innerHTML =
            '<div class="toast show align-items-center text-white bg-' +
            (type === "warning" ? "warning text-dark" : "primary") +
            ' border-0" role="alert">' +
            '<div class="d-flex">' +
            '<div class="toast-body fw-bold">' + message + '</div>' +
            '<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>' +
            '</div></div>';
        document.body.appendChild(toast);
        setTimeout(function () { toast.remove(); }, 6000);
    }

    // ── Browser Notification API ───────────────────────────────────
    function showBrowserNotification(title, body) {
        if (!("Notification" in window)) return;
        if (Notification.permission === "granted") {
            try { new Notification(title, { body: body }); } catch (_) { }
        }
    }

    function requestBrowserNotificationPermission() {
        if (!("Notification" in window)) return;
        if (Notification.permission === "default") {
            Notification.requestPermission().catch(function () { });
        }
    }

    if (isCustomer) {
        requestBrowserNotificationPermission();
    }

    // ── Polling fallback (chỉ chạy khi SignalR không connect được) ─
    var signalRConnected = false;
    connection.onreconnected(function () { signalRConnected = true; });
    connection.onclose(function () { signalRConnected = false; });

    if (isCustomer && publicToken) {
        // Snapshot lần đầu để polling không bắn toast trùng.
        var currentStatusEl = document.getElementById("statusText");
        if (currentStatusEl) lastStatus = currentStatusEl.textContent;
        var currentAheadEl = document.getElementById("peopleAhead");
        if (currentAheadEl) {
            var m = currentAheadEl.textContent.match(/\d+/);
            if (m) lastPeopleAhead = parseInt(m[0], 10);
        }

        setInterval(function () {
            if (signalRConnected) return;
            refreshFromServer();
        }, 15000);
    }
})();