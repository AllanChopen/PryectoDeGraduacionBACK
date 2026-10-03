ALTER TABLE banda
    ADD COLUMN IF NOT EXISTS recurrente_subscription_checkout_id VARCHAR(100),
    ADD COLUMN IF NOT EXISTS recurrente_subscription_id VARCHAR(100),
    ADD COLUMN IF NOT EXISTS suscripcion_estado VARCHAR(30),
    ADD COLUMN IF NOT EXISTS suscripcion_fin_periodo TIMESTAMPTZ,
    ADD COLUMN IF NOT EXISTS suscripcion_setup_confirmado BOOLEAN NOT NULL DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS onboarding_completado BOOLEAN NOT NULL DEFAULT FALSE;

CREATE UNIQUE INDEX IF NOT EXISTS ux_banda_subscription_checkout
    ON banda (recurrente_subscription_checkout_id)
    WHERE recurrente_subscription_checkout_id IS NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ux_banda_subscription_id
    ON banda (recurrente_subscription_id)
    WHERE recurrente_subscription_id IS NOT NULL;
