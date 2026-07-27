// Gestión de aplicaciones (US4): consume /api/aplicaciones same-origin.
(function () {
    "use strict";

    const tbody = document.getElementById("aplicaciones-tbody");
    const form = document.getElementById("crear-aplicacion-form");
    const nombreInput = document.getElementById("nombre");
    const urlInput = document.getElementById("url");
    const errorBox = document.getElementById("aplicacion-error");

    async function cargar() {
        const response = await fetch("/api/aplicaciones");
        if (!response.ok) {
            return;
        }

        const aplicaciones = await response.json();
        tbody.innerHTML = "";
        aplicaciones.forEach(renderFila);
    }

    function renderFila(app) {
        const tr = document.createElement("tr");

        const nombre = document.createElement("td");
        nombre.textContent = app.nombre;
        const url = document.createElement("td");
        url.textContent = app.url;

        const acciones = document.createElement("td");
        acciones.className = "text-end";

        const editar = document.createElement("button");
        editar.className = "btn btn-sm btn-outline-secondary me-2";
        editar.textContent = "Editar";
        editar.addEventListener("click", () => onEditar(app));

        const eliminar = document.createElement("button");
        eliminar.className = "btn btn-sm btn-outline-danger";
        eliminar.textContent = "Eliminar";
        eliminar.addEventListener("click", () => onEliminar(app));

        acciones.append(editar, eliminar);
        tr.append(nombre, url, acciones);
        tbody.appendChild(tr);
    }

    async function onEditar(app) {
        const nombre = window.prompt("Nombre:", app.nombre);
        if (nombre === null) {
            return;
        }
        const url = window.prompt("URL:", app.url);
        if (url === null) {
            return;
        }

        const response = await fetch(`/api/aplicaciones/${app.id}`, {
            method: "PUT",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ nombre, url })
        });

        if (!response.ok) {
            mostrarError("No se pudo editar la aplicación (¿URL vacía?).");
            return;
        }
        await cargar();
    }

    async function onEliminar(app) {
        if (!window.confirm(`¿Eliminar la aplicación ${app.nombre}?`)) {
            return;
        }
        await fetch(`/api/aplicaciones/${app.id}`, { method: "DELETE" });
        await cargar();
    }

    function mostrarError(mensaje) {
        errorBox.textContent = mensaje;
        errorBox.classList.remove("d-none");
    }

    form.addEventListener("submit", async function (event) {
        event.preventDefault();
        errorBox.classList.add("d-none");

        const payload = { nombre: nombreInput.value.trim(), url: urlInput.value.trim() };
        const response = await fetch("/api/aplicaciones", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        if (response.ok) {
            nombreInput.value = "";
            urlInput.value = "";
            await cargar();
            return;
        }

        mostrarError("No se pudo registrar la aplicación: la URL no puede estar vacía.");
    });

    cargar();
})();
