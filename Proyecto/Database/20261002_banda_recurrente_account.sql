-- Ejecutar si la tabla banda aún no tiene los campos de Recurrente.
ALTER TABLE banda
    ADD COLUMN IF NOT EXISTS recurrente_account_id VARCHAR(100),
    ADD COLUMN IF NOT EXISTS recurrente_account_status VARCHAR(100),
    ADD COLUMN IF NOT EXISTS recurrente_onboarding_completed BOOLEAN NOT NULL DEFAULT FALSE;
