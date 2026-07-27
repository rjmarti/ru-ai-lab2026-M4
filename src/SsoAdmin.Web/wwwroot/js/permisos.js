// Gestión de permisos (US4): consume /api/permisos, /api/usuarios y /api/aplicaciones.
(function () {
    "use strict";

    const tbody = document.getElementById("permisos-tbody");
    const form = document.getElementById("otorgar-permiso-form");
    const usuarioSelect = document.getElementById("usuario-select");
    const aplicacionSelect = document.getElementById("aplicacion-select");
    const fechaDesde = document.getElementById("fecha-desde");
    const fechaHasta = document.getElementById("fecha-hasta");
    const errorBox = document.getElementById("permiso-error");

    const usuarios = new Map();
    const aplicaciones = new Map();

    async function cargarSelects() {
        const [usuariosResp, appsResp] = await Promise.all([
            fetch("/api/usuarios"),
            fetch("/api/aplicaciones")
        ]);

        if (usuariosResp.ok) {
            const data = await usuariosResp.json();
            usuarioSelect.innerHTML = "";
            usuarios.clear();
            data.forEach(function (u) {
                usuarios.set(u.id, u.nombre);
                const option = document.createElement("option");
                option.value = u.id;
                option.textContent = u.nombre;
                usuarioSelect.appendChild(option);
            });
        }

        if (appsResp.ok) {
            const data = await appsResp.json();
            aplicacionSelect.innerHTML = "";
            aplicaciones.clear();
            data.forEach(function (a) {
                aplicaciones.set(a.id, a.nombre);
                const option = document.createElement("option");
                option.value = a.id;
                option.textContent = a.nombre;
                aplicacionSelect.appendChild(option);
            });
        }
    }

    async function cargarPermisos() {
        const response = await fetch("/api/permisos");
        if (!response.ok) {
            return;
        }

        const permisos = await response.json();
        tbody.innerHTML = "";
        permisos.forEach(renderFila);
    }

    function renderFila(permiso) {
        const tr = document.createElement("tr");

        const usuario = document.createElement("td");
        usuario.textContent = usuarios.get(permiso.usuarioId) || permiso.usuarioId;
        const aplicacion = document.createElement("td");
        aplicacion.textContent = aplicaciones.get(permiso.aplicacionId) || permiso.aplicacionId;
        const desde = document.createElement("td");
        desde.textContent = permiso.fechaDesde;
        const hasta = document.createElement("td");
        hasta.textContent = permiso.fechaHasta || "indefinido";

        const acciones = document.createElement("td");
        acciones.className = "text-end";
        const revocar = document.createElement("button");
        revocar.className = "btn btn-sm btn-outline-danger";
        revocar.textContent = "Revocar";
        revocar.addEventListener("click", () => onRevocar(permiso));
        acciones.appendChild(revocar);

        tr.append(usuario, aplicacion, desde, hasta, acciones);
        tbody.appendChild(tr);
    }

    async function onRevocar(permiso) {
        if (!window.confirm("¿Revocar este permiso?")) {
            return;
        }
        await fetch(`/api/permisos/${permiso.id}/revocar`, { method: "POST" });
        await cargarPermisos();
    }

    form.addEventListener("submit", async function (event) {
        event.preventDefault();
        errorBox.classList.add("d-none");

        const payload = {
            usuarioId: usuarioSelect.value,
            aplicacionId: aplicacionSelect.value,
            fechaDesde: fechaDesde.value,
            fechaHasta: fechaHasta.value || null
        };

        const response = await fetch("/api/permisos", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        if (response.ok) {
            fechaHasta.value = "";
            await cargarPermisos();
            return;
        }

        errorBox.textContent = response.status === 409
            ? "El período se solapa con un permiso existente."
            : "No se pudo otorgar el permiso (revise las fechas).";
        errorBox.classList.remove("d-none");
    });

    (async function init() {
        await cargarSelects();
        await cargarPermisos();
    })();
})();
