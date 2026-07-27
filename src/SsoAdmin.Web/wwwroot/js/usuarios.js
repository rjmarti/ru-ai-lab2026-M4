// Gestión de usuarios (US2): consume la API interna same-origin /api/usuarios.
(function () {
    "use strict";

    const tbody = document.getElementById("usuarios-tbody");
    const form = document.getElementById("crear-usuario-form");
    const nombreInput = document.getElementById("nuevo-nombre");

    async function cargar() {
        const response = await fetch("/api/usuarios");
        if (!response.ok) {
            return;
        }

        const usuarios = await response.json();
        tbody.innerHTML = "";
        usuarios.forEach(renderFila);
    }

    function renderFila(usuario) {
        const tr = document.createElement("tr");

        const nombre = document.createElement("td");
        nombre.textContent = usuario.nombre;

        const estado = document.createElement("td");
        const badge = document.createElement("span");
        badge.className = usuario.activo ? "badge bg-success" : "badge bg-secondary";
        badge.textContent = usuario.activo ? "Activo" : "Inactivo";
        estado.appendChild(badge);

        const acciones = document.createElement("td");
        acciones.className = "text-end";

        const editar = document.createElement("button");
        editar.className = "btn btn-sm btn-outline-secondary me-2";
        editar.textContent = "Editar";
        editar.addEventListener("click", () => onEditar(usuario));
        acciones.appendChild(editar);

        if (usuario.activo) {
            const baja = document.createElement("button");
            baja.className = "btn btn-sm btn-outline-danger";
            baja.textContent = "Dar de baja";
            baja.addEventListener("click", () => onBaja(usuario));
            acciones.appendChild(baja);
        }

        tr.append(nombre, estado, acciones);
        tbody.appendChild(tr);
    }

    async function onEditar(usuario) {
        const nuevoNombre = window.prompt("Nuevo nombre:", usuario.nombre);
        if (!nuevoNombre) {
            return;
        }

        await fetch(`/api/usuarios/${usuario.id}`, {
            method: "PUT",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ nombre: nuevoNombre })
        });
        await cargar();
    }

    async function onBaja(usuario) {
        if (!window.confirm(`¿Dar de baja a ${usuario.nombre}? Se caducarán todos sus permisos activos.`)) {
            return;
        }

        await fetch(`/api/usuarios/${usuario.id}/baja`, { method: "POST" });
        await cargar();
    }

    form.addEventListener("submit", async function (event) {
        event.preventDefault();
        const nombre = nombreInput.value.trim();
        if (!nombre) {
            return;
        }

        const response = await fetch("/api/usuarios", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ nombre })
        });

        if (response.ok) {
            nombreInput.value = "";
            await cargar();
        }
    });

    cargar();
})();
