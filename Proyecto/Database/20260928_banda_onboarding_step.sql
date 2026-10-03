-- Las bandas existentes ya tienen una página creada.
ALTER TABLE banda ADD COLUMN onboarding_step integer NOT NULL DEFAULT 4;
