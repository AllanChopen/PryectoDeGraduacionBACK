-- Ejecutar antes de desplegar el backend que lee orden.banda_id.
-- Las órdenes de eventos conservan banda_id NULL; los endpoints de merch las excluyen.
BEGIN;

ALTER TABLE orden ADD COLUMN IF NOT EXISTS banda_id integer;

-- Solo asociar órdenes existentes cuando todos sus productos aún existen
-- y pertenecen a la misma banda. Las órdenes ambiguas quedan sin acceso
-- en el dashboard hasta corregirlas manualmente.
UPDATE orden AS o
SET banda_id = origen.banda_id
FROM (
    SELECT d.orden_id, MIN(p.banda_id) AS banda_id
    FROM orden_detalle AS d
    LEFT JOIN producto AS p ON p.id = d.producto_id
    GROUP BY d.orden_id
    HAVING COUNT(*) = COUNT(p.id) AND COUNT(DISTINCT p.banda_id) = 1
) AS origen
WHERE o.id = origen.orden_id
  AND o.evento_id IS NULL
  AND o.banda_id IS NULL;

CREATE INDEX IF NOT EXISTS ix_orden_banda_merch_pagada
    ON orden (banda_id, created_at DESC)
    WHERE evento_id IS NULL AND estado_pago = 'pagado';

COMMIT;
