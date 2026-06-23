/// <reference path="~/lib/jquery/dist/jquery.min.js" />

(function () {
    if (!window.queueLinkConfig) return;

    var cfg = window.queueLinkConfig;
    var isCustomer = cfg.isCustomer;
    var isStaff = cfg.isStaff;
    var queueServiceId = cfg.queueServiceId;
    var publicToken = cfg.publicToken;

    // Connect to SignalR hub.
    var connection = new signalR.HubConnectionBuilder()
        .withUrl("/queueHub")
        .withAutomaticReconnect()
        .build();

    // ── Customer: join ticket group ──────────────────────────────
    if (isCustomer && publicToken) {
        connection.start().then(function () {
            connection.invoke("JoinTicketGroup", publicToken);
        }).catch(function (err) {
            console.warn("SignalR connect failed:", err);
        });
    }

    // ── Staff: join queue group ────────────────────────────────
    if (isStaff && queueServiceId) {
        connection.start().then(function () {
            connection.invoke("JoinQueueGroup", queueServiceId);
        }).catch(function (err) {
            console.warn("SignalR connect failed:", err);
        });
    }

    // ── TicketUpdated event ───────────────────────────────────
    connection.on("TicketUpdated", function (data) {
        // For customers: reload page to reflect new status.
        if (isCustomer && data.publicToken === publicToken) {
            location.reload();
            return;
        }

        // For staff dashboard: reload to show new data.
        if (isStaff && data.queueServiceId === queueServiceId) {
            location.reload();
        }
    });

    // ── QueueUpdated event ────────────────────────────────────
    connection.on("QueueUpdated", function (data) {
        if (isStaff && data.queueServiceId === queueServiceId) {
            location.reload();
        }
    });

    // ── CurrentlyCallingChanged event ───────────────────────────
    connection.on("CurrentlyCallingChanged", function (data) {
        if (isCustomer && data.queueServiceId === queueServiceId) {
            // Show a toast/alert without full reload for the customer.
            showToast("Đang gọi số: " + data.ticketCode, "warning");
        }

        if (isStaff && data.queueServiceId === queueServiceId) {
            var el = document.getElementById("currentCall");
            if (el) el.textContent = data.ticketCode;
        }
    });

    // ── Toast helper (no bootstrap dependency needed) ──────────
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
        setTimeout(function () {
            document.body.removeChild(toast);
        }, 6000);
    }

    // ── Auto-refresh fallback (polling every 15s) ─────────────
    if (isCustomer && publicToken) {
        setInterval(function () {
            fetch("/Queue/GetTicketStatus?token=" + encodeURIComponent(publicToken))
                .then(function (r) { return r.json(); })
                .then(function (data) {
                    // Only reload if status changed.
                    var currentStatus = document.getElementById("statusText");
                    if (currentStatus && currentStatus.textContent !== data.statusText) {
                        location.reload();
                    }
                })
                .catch(function () { });
        }, 15000);
    }
})();
