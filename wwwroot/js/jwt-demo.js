// =============================================================================
// JWT Demo client
// A minimal "front end" that shows the client side of token authentication:
//   1. POST credentials to /api/auth/login and receive a JWT
//   2. Store the JWT (localStorage here)
//   3. Send it as "Authorization: Bearer <token>" on later requests
//   4. "Log out" by deleting it
// =============================================================================

const TOKEN_KEY = "jwt";
let expiryTimer = null;

const $ = (id) => document.getElementById(id);

// ---------------------------------------------------------------------------
// Base64URL helpers. A JWT segment is Base64 with "-" for "+", "_" for "/",
// and no "=" padding. Anyone can decode it - no key required.
// ---------------------------------------------------------------------------
function base64UrlDecode(segment) {
    let base64 = segment.replace(/-/g, "+").replace(/_/g, "/");
    while (base64.length % 4) base64 += "=";
    const bytes = Uint8Array.from(atob(base64), (c) => c.charCodeAt(0));
    return new TextDecoder().decode(bytes);
}

function base64UrlEncode(text) {
    const bytes = new TextEncoder().encode(text);
    let binary = "";
    bytes.forEach((b) => (binary += String.fromCharCode(b)));
    return btoa(binary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}

function decodeToken(token) {
    const [header, payload] = token.split(".");
    return {
        header: JSON.parse(base64UrlDecode(header)),
        payload: JSON.parse(base64UrlDecode(payload))
    };
}

// ---------------------------------------------------------------------------
// Token display
// ---------------------------------------------------------------------------
function escapeHtml(s) {
    return s.replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
}

function renderToken() {
    const token = localStorage.getItem(TOKEN_KEY);
    clearInterval(expiryTimer);

    if (!token) {
        $("rawToken").innerHTML = '<em class="text-muted">Log in to receive a token.</em>';
        $("decodedHeader").textContent = "";
        $("decodedPayload").textContent = "";
        $("expiry").className = "badge text-bg-secondary";
        $("expiry").textContent = "No token";
        return;
    }

    const [h, p, s] = token.split(".");
    $("rawToken").innerHTML =
        `<span class="jwt-header">${escapeHtml(h)}</span>.` +
        `<span class="jwt-payload">${escapeHtml(p)}</span>.` +
        `<span class="jwt-signature">${escapeHtml(s)}</span>`;

    const { header, payload } = decodeToken(token);
    $("decodedHeader").textContent = JSON.stringify(header, null, 2);

    // Show the Unix timestamps as readable dates too.
    const annotated = { ...payload };
    for (const claim of ["exp", "iat", "nbf"]) {
        if (typeof payload[claim] === "number") {
            annotated[claim] = `${payload[claim]}  (${new Date(payload[claim] * 1000).toLocaleString()})`;
        }
    }
    $("decodedPayload").textContent = JSON.stringify(annotated, null, 2);

    const tick = () => {
        const secondsLeft = Math.round(payload.exp - Date.now() / 1000);
        if (secondsLeft > 0) {
            const m = Math.floor(secondsLeft / 60), sec = String(secondsLeft % 60).padStart(2, "0");
            $("expiry").className = "badge text-bg-success";
            $("expiry").textContent = `Expires in ${m}:${sec}`;
        } else {
            $("expiry").className = "badge text-bg-danger";
            $("expiry").textContent = "Expired";
            clearInterval(expiryTimer);
        }
    };
    tick();
    expiryTimer = setInterval(tick, 1000);
}

// ---------------------------------------------------------------------------
// Login / logout
// ---------------------------------------------------------------------------
async function login(event) {
    event.preventDefault();
    const status = $("loginStatus");

    const response = await fetch("/api/auth/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email: $("email").value, password: $("password").value })
    });

    if (!response.ok) {
        localStorage.removeItem(TOKEN_KEY);
        renderToken();
        status.innerHTML = `<div class="alert alert-danger py-2 mb-0">${response.status} ${response.statusText}: ${escapeHtml(await response.text())}</div>`;
        return;
    }

    const { token } = await response.json();
    localStorage.setItem(TOKEN_KEY, token);
    status.innerHTML = '<div class="alert alert-success py-2 mb-0">Logged in. Token saved to <code>localStorage</code>.</div>';
    renderToken();
}

function logout() {
    // Client-side deletion: the simplest logout strategy. The token itself is
    // still valid until "exp" - the server has no session to destroy.
    localStorage.removeItem(TOKEN_KEY);
    $("loginStatus").innerHTML = '<div class="alert alert-secondary py-2 mb-0">Token deleted from <code>localStorage</code>.</div>';
    renderToken();
}

// ---------------------------------------------------------------------------
// Calling the API
// ---------------------------------------------------------------------------
async function callApi(method, url, tokenOverride) {
    const token = tokenOverride ?? localStorage.getItem(TOKEN_KEY);
    const headers = {};
    if ($("sendToken").checked && token) {
        headers["Authorization"] = `Bearer ${token}`;
    }

    // Show the request the way it goes over the wire.
    let req = `${method} ${url} HTTP/1.1\nHost: ${location.host}`;
    if (headers.Authorization) req += `\nAuthorization: ${headers.Authorization}`;
    else req += "\n(no Authorization header)";
    $("requestOut").textContent = req;

    const response = await fetch(url, { method, headers });

    let out = `HTTP/1.1 ${response.status} ${response.statusText}`;
    const wwwAuth = response.headers.get("WWW-Authenticate");
    if (wwwAuth) out += `\nWWW-Authenticate: ${wwwAuth}`;

    const body = await response.text();
    if (body) {
        try { out += "\n\n" + JSON.stringify(JSON.parse(body), null, 2); }
        catch { out += "\n\n" + body; }
    }

    $("responseOut").textContent = out;
    $("responseOut").className = "jwt-box " + (response.ok ? "jwt-ok" : "jwt-fail");
}

function tamper() {
    const token = localStorage.getItem(TOKEN_KEY);
    if (!token) {
        alert("Log in first (try user@example.com).");
        return;
    }
    const [header, payload, signature] = token.split(".");
    const claims = JSON.parse(base64UrlDecode(payload));
    claims.role = "Administrator";                       // escalate privileges...
    const forged = `${header}.${base64UrlEncode(JSON.stringify(claims))}.${signature}`; // ...but reuse old signature
    $("sendToken").checked = true;
    callApi("DELETE", "/api/products/1", forged);
}

// ---------------------------------------------------------------------------
// Wire up the page
// ---------------------------------------------------------------------------
document.addEventListener("DOMContentLoaded", () => {
    $("loginForm").addEventListener("submit", login);
    $("logoutBtn").addEventListener("click", logout);
    $("tamperBtn").addEventListener("click", tamper);

    document.querySelectorAll("[data-email]").forEach((btn) =>
        btn.addEventListener("click", () => {
            $("email").value = btn.dataset.email;
            $("password").value = "Password123!";
        }));

    document.querySelectorAll("button[data-url]").forEach((btn) =>
        btn.addEventListener("click", () => callApi(btn.dataset.method, btn.dataset.url)));

    renderToken(); // a token from a previous visit survives a page refresh
});
