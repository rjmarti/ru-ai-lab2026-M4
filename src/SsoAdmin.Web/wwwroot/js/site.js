// Comportamiento común del sitio de administración.
(function () {
    "use strict";

    const logoutButton = document.getElementById("logout-btn");
    if (logoutButton) {
        logoutButton.addEventListener("click", async function () {
            await fetch("/api/auth/logout", { method: "POST" });
            window.location.href = "/Login";
        });
    }
})();
