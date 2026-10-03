# Recuperación de contraseña

El frontend necesita dos pantallas: **Olvidé mi contraseña** y **Ingresar
código y contraseña nueva**. Ambos endpoints son públicos y reciben JSON.

## 1. Solicitar código

`POST /api/Auth/forgot-password`

```json
{
  "email": "contacto@banda.com"
}
```

La respuesta `200` es la misma si el correo existe o no:

```json
{
  "message": "Si el correo está registrado, recibirás un código para restablecer tu contraseña."
}
```

Si existe una cuenta, llega un código de **6 dígitos** por correo. Vence en
**10 minutos**. El servidor permite solicitar otro código después de **60
segundos**; al pedir uno nuevo, el anterior deja de servir. El frontend puede
mostrar la pantalla para ingresar el código tras recibir `200` y habilitar
**Reenviar código** después de 60 segundos.

## 2. Confirmar código y nueva contraseña

`POST /api/Auth/reset-password`

```json
{
  "email": "contacto@banda.com",
  "code": "042781",
  "newPassword": "mi-nueva-contraseña",
  "confirmPassword": "mi-nueva-contraseña"
}
```

La contraseña debe tener al menos **8 caracteres**. Un código incorrecto,
vencido o agotado devuelve `400` con `message: "Código inválido o vencido."`.
El código permite **5 intentos**. Si las contraseñas no coinciden, la respuesta
es `400` con un mensaje específico.

Al recibir `200`, borrar cualquier JWT guardado en el frontend y dirigir a
`/login`. El backend invalida los tokens emitidos antes del cambio. El código
se consume al usarlo correctamente.

## Ejemplo con `fetch`

```js
const API = "https://URL-DEL-BACKEND";

async function postJson(path, body) {
  const response = await fetch(`${API}${path}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  const result = await response.json();
  if (!response.ok) throw new Error(result.message ?? "No se pudo completar la solicitud");
  return result;
}

await postJson("/api/Auth/forgot-password", { email });
// Mostrar la pantalla del código y conservar el email en el estado del flujo.

await postJson("/api/Auth/reset-password", {
  email,
  code,
  newPassword,
  confirmPassword,
});
// Borrar la sesión local y dirigir a /login.
```

Conservar el código en el estado del formulario; no incluirlo en la URL. Si el
envío de correo falla, la solicitud puede devolver `503` para permitir reintento.

## Despliegue

Aplicar `Proyecto/Database/20260928_password_reset.sql` antes de desplegar el
backend actualizado. Configurar `MailSettings` con el servidor SMTP y el
remitente de producción.
