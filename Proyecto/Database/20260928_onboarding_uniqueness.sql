-- Resolver duplicados existentes antes de ejecutar este script.
CREATE UNIQUE INDEX IF NOT EXISTS ux_usuario_email_lower ON usuario (lower(email));
CREATE UNIQUE INDEX IF NOT EXISTS ux_banda_slug_lower ON banda (lower(slug));
CREATE UNIQUE INDEX IF NOT EXISTS ux_banda_usuario_id ON banda (usuario_id);
