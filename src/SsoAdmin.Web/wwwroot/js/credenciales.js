// Gestión de credenciales (US3): consume /api/credenciales y /api/usuarios same-origin.
(function () {
    "use strict";

    const tbody = document.getElementById("credenciales-tbody");
    const form = document.getElementById("crear-credencial-form");
    const usuarioSelect = document.getElementById("usuario-select");
    const usernameInput = document.getElementById("username");
    const emisorInput = document.getElementById("emisor");
    const errorBox = document.getElementById("credencial-error");

    async function cargarUsuarios() {
        const response = await fetch("/api/usuarios");
        if (!response.ok) {
            return;
        }

        const usuarios = await response.json();
        usuarioSelect.innerHTML = "";
        usuarios.forEach(function (usuario) {
            const option = document.createElement("option");
            option.value = usuario.id;
            option.textContent = usuario.nombre;
            usuarioSelect.appendChild(option);
        });
    }

    async function cargarCredenciales() {
        const response = await fetch("/api/credenciales");
        if (!response.ok) {
            return;
        }

        const credenciales = await response.json();
        tbody.innerHTML = "";
        credenciales.forEach(renderFila);
    }

    function renderFila(credencial) {
        const tr = document.createElement("tr");

        const usuario = document.createElement("td");
        usuario.textContent = credencial.usuarioNombre;
        const username = document.createElement("td");
        username.textContent = credencial.username;
        const emisor = document.createElement("td");
        emisor.textContent = credencial.emisor;

        const acciones = document.createElement("td");
        acciones.className = "text-end";
        const eliminar = document.createElement("button");
        eliminar.className = "btn btn-sm btn-outline-danger";
        eliminar.textContent = "Eliminar";
        eliminar.addEventListener("click", () => onEliminar(credencial));
        acciones.appendChild(eliminar);

        tr.append(usuario, username, emisor, acciones);
        tbody.appendChild(tr);
    }

    async function onEliminar(credencial) {
        if (!window.confirm(`¿Eliminar la credencial ${credencial.username}/${credencial.emisor}?`)) {
            return;
        }

        await fetch(`/api/credenciales/${credencial.id}`, { method: "DELETE" });
        await cargarCredenciales();
    }

    form.addEventListener("submit", async function (event) {
        event.preventDefault();
        errorBox.classList.add("d-none");

        const payload = {
            usuarioId: usuarioSelect.value,
            username: usernameInput.value.trim(),
            emisor: emisorInput.value.trim()
        };

        const response = await fetch("/api/credenciales", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        if (response.ok) {
            usernameInput.value = "";
            emisorInput.value = "";
            await cargarCredenciales();
            return;
        }

        errorBox.textContent = "No se pudo crear la credencial: la combinación de username y emisor ya está en uso.";
        errorBox.classList.remove("d-none");
    });

    cargarUsuarios();
    cargarCredenciales();
})();
