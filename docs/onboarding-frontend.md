# Onboarding de bandas: contrato para frontend

El flujo usa `https://backstage.gt/{slug}` como ruta pública. La API devuelve
`publicPath` (`/{slug}`); el frontend añade su propio dominio. Todas las rutas
son relativas a la URL base del backend.

## 1. Cuenta Backstage

`POST /api/onboarding/registro` con `Content-Type: application/json`:

```json
{
  "email": "contacto@banda.com",
  "password": "contraseña-segura",
  "confirmPassword": "contraseña-segura"
}
```

Devuelve `201` con `token`, `user` y `onboardingStep: 2`. Guardar el token
para las siguientes llamadas. El servidor asigna el rol `bandadmin`.
Contraseñas distintas producen `400`; correo repetido, `409`.

Si la persona ya tiene cuenta, usar `POST /api/Auth/login` con `email` y
`password`. Luego consultar `GET /api/onboarding/estado` para retomar.

## 2. Identidad de la banda

`POST /api/onboarding/identidad`, con `Authorization: Bearer <token>` y
`multipart/form-data`:

| Campo | Tipo | Obligatorio |
| --- | --- | --- |
| `Nombre` | texto | Sí |
| `Slug` | texto | Sí |
| `Logo` | archivo de imagen | Sí |

El slug se normaliza a minúsculas y admite letras, números y guiones internos.
La respuesta `201` contiene `banda`, `publicPath`, `onboardingStep: 3` y **un
token nuevo** con `BandaId`. Reemplazar el token anterior antes de llamar a
redes sociales u otros endpoints de la banda. Un slug ocupado produce `409`.

```json
{
  "token": "<nuevo-jwt>",
  "banda": {
    "id": 6,
    "uuid": "0e4d2c82-4f30-4eac-a14a-775217dc68e3",
    "nombre": "Lost in the Ocean",
    "slug": "lost-in-the-ocean",
    "logoUrl": "https://bucket.s3.amazonaws.com/bandas/logo.png"
  },
  "publicPath": "/lost-in-the-ocean",
  "onboardingStep": 3
}
```

Para el botón **Atrás** desde el paso 3, usar `PUT /api/onboarding/identidad`
con `Nombre`, `Slug` y `Logo` opcional. La respuesta trae la ruta actualizada.

## 3. Personalizar

`PUT /api/onboarding/personalizacion` con Bearer y `multipart/form-data`:

| Campo | Tipo | Efecto |
| --- | --- | --- |
| `Imagen` | archivo | Portada |
| `BiografiaImagen` | archivo | Imagen de biografía |
| `Descripcion` | texto | Descripción corta |
| `Biografia` | texto | Biografía |
| `Genero` | texto | Género |
| `PrecioEnvio` | número >= 0 | Precio de envío |

Los campos omitidos conservan su valor. Se puede enviar una cadena vacía para
limpiar un texto. Esta llamada marca `onboardingStep: 4`, incluso si la banda
decide omitir todos los campos opcionales.

Las redes se gestionan en el mismo paso con `GET /api/RedSocial`,
`POST /api/RedSocial`, `PUT /api/RedSocial/{id}` y
`DELETE /api/RedSocial/{id}`. `POST` y `PUT` aceptan JSON como:

```json
{
  "tipo": "instagram",
  "nombre": "Instagram",
  "url": "https://www.instagram.com/mi_banda"
}
```

Estos endpoints usan el `BandaId` del token. El frontend puede guardar las
redes una a una y luego llamar a `personalizacion` al pulsar **Continuar**.

## 4. Preview

`GET /api/onboarding/estado` devuelve `onboardingStep`, `banda` y
`publicPath`. Con `onboardingStep: 4`, abrir `publicPath` en el frontend.
Si la cuenta aún no tiene banda, devuelve `onboardingStep: 2` y `banda: null`.
La página puede leer `GET /api/Bandas/public/{slug}` y
`GET /api/RedSocial/public/{slug}`. La primera respuesta contiene `logoUrl`,
`imagenUrl`, `biografiaImagenUrl`, biografía, género y precio de envío. El
frontend puede usar `logoUrl` para el logo de la página y el favicon.

El siguiente botón, **Configurar cobros**, pertenece a la siguiente etapa del
producto. La URL pública existe desde que se crea la identidad de la banda;
este flujo todavía no tiene un estado de publicación privado.

## Ejemplo mínimo de llamadas con `fetch`

```js
const API = "https://URL-DEL-BACKEND";

async function readJson(response) {
  const data = await response.json();
  if (!response.ok) throw new Error(data.message ?? "No se pudo guardar");
  return data;
}

const registro = await readJson(await fetch(`${API}/api/onboarding/registro`, {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ email, password, confirmPassword }),
}));
let token = registro.token;

const identidad = new FormData();
identidad.append("Nombre", nombre);
identidad.append("Slug", slug);
identidad.append("Logo", logoFile);
const resultado = await readJson(await fetch(`${API}/api/onboarding/identidad`, {
  method: "POST",
  headers: { Authorization: `Bearer ${token}` },
  body: identidad,
}));
token = resultado.token;

const personalizacion = new FormData();
personalizacion.append("Biografia", biografia);
if (portadaFile) personalizacion.append("Imagen", portadaFile);
await readJson(await fetch(`${API}/api/onboarding/personalizacion`, {
  method: "PUT",
  headers: { Authorization: `Bearer ${token}` },
  body: personalizacion,
}));
```

Al usar `FormData`, dejar que el navegador agregue el `Content-Type` con su
boundary. Para producción, configurar `Frontend:Origins` con los orígenes
reales del frontend.

## Cambios de base de datos necesarios

Aplicar, antes de desplegar el backend, los scripts
`Proyecto/Database/20260928_banda_logo_url.sql`,
`Proyecto/Database/20260928_banda_onboarding_step.sql` y
`Proyecto/Database/20260928_onboarding_uniqueness.sql`.
El último crea índices únicos para correo, slug y usuario de la banda;
si ya existen duplicados, hay que resolverlos antes de ejecutarlo.
