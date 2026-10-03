ALTER TABLE usuario ADD COLUMN token_version integer NOT NULL DEFAULT 0;

CREATE TABLE password_reset_code (
    usuario_id integer PRIMARY KEY REFERENCES usuario(id) ON DELETE CASCADE,
    code_hash bytea NOT NULL,
    requested_at timestamp with time zone NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    attempts integer NOT NULL DEFAULT 0
);
