// Login de SI: postea las credenciales a la API interna same-origin y, si son válidas,
// redirige a la gestión de usuarios (US2).
(function () {
    "use strict";

    const form = document.getElementById("login-form");
    const errorBox = document.getElementById("login-error");

    form.addEventListener("submit", async function (event) {
        event.preventDefault();
        errorBox.classList.add("d-none");

        const payload = {
            usuario: document.getElementById("usuario").value,
            password: document.getElementById("password").value
        };

        const response = await fetch("/api/auth/login", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        if (response.ok) {
            window.location.href = "/Usuarios";
            return;
        }

        errorBox.textContent = "Usuario o contraseña inválidos.";
        errorBox.classList.remove("d-none");
    });
})();
