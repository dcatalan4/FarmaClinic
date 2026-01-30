# TABLA DE REFERENCIA - NOMBRES CORRECTOS DE TABLAS Y COLUMNAS

## 📋 Tablas en PostgreSQL (según tu script SQL)

### 1. usuario
```sql
CREATE TABLE usuario (
    id_usuario SERIAL PRIMARY KEY,
    usuario VARCHAR(50) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    nombre VARCHAR(100),
    rol VARCHAR(20) NOT NULL,
    activo BOOLEAN DEFAULT TRUE,
    fecha_creacion TIMESTAMP DEFAULT NOW()
);
```

### 2. producto
```sql
CREATE TABLE producto (
    id_producto SERIAL PRIMARY KEY,
    codigo VARCHAR(50) NOT NULL UNIQUE,
    nombre VARCHAR(150) NOT NULL,
    descripcion VARCHAR(255),
    precio_ingreso NUMERIC(10,2) NOT NULL,
    precio_venta NUMERIC(10,2) NOT NULL,
    stock_actual INT NOT NULL,
    activo BOOLEAN DEFAULT TRUE,
    fecha_creacion TIMESTAMP DEFAULT NOW()
);
```

### 3. caja
```sql
CREATE TABLE caja (
    id_caja SERIAL PRIMARY KEY,
    nombre VARCHAR(50) NOT NULL,
    saldo_actual NUMERIC(10,2) NOT NULL,
    activa BOOLEAN DEFAULT TRUE,
    fecha_creacion TIMESTAMP DEFAULT NOW()
);
```

### 4. venta
```sql
CREATE TABLE venta (
    id_venta SERIAL PRIMARY KEY,
    numero_venta INT NOT NULL UNIQUE,
    fecha TIMESTAMP DEFAULT NOW(),
    id_usuario INT NOT NULL,
    subtotal NUMERIC(10,2) NOT NULL,
    total NUMERIC(10,2) NOT NULL,
    anulada BOOLEAN DEFAULT FALSE,
    activa BOOLEAN DEFAULT TRUE
);
```

### 5. detalle_venta
```sql
CREATE TABLE detalle_venta (
    id_detalle_venta SERIAL PRIMARY KEY,
    id_venta INT NOT NULL,
    id_producto INT NOT NULL,
    cantidad INT NOT NULL,
    precio_unitario NUMERIC(10,2) NOT NULL,
    precio_ingreso NUMERIC(10,2) NOT NULL,
    subtotal NUMERIC(10,2) NOT NULL
);
```

### 6. movimiento_inventario
```sql
CREATE TABLE movimiento_inventario (
    id_movimiento SERIAL PRIMARY KEY,
    id_producto INT NOT NULL,
    tipo_movimiento CHAR(1) NOT NULL,
    cantidad INT NOT NULL,
    fecha TIMESTAMP DEFAULT NOW(),
    referencia VARCHAR(50),
    id_referencia INT,
    id_usuario INT NOT NULL
);
```

### 7. movimiento_caja
```sql
CREATE TABLE movimiento_caja (
    id_movimiento_caja SERIAL PRIMARY KEY,
    id_caja INT NOT NULL,
    tipo_movimiento CHAR(1) NOT NULL,
    monto NUMERIC(10,2) NOT NULL,
    fecha TIMESTAMP DEFAULT NOW(),
    concepto VARCHAR(50),
    id_referencia INT,
    id_usuario INT NOT NULL,
    saldo_en_momento NUMERIC(10,2) DEFAULT 0
);
```

### 8. saldo_caja_diario
```sql
CREATE TABLE saldo_caja_diario (
    id_saldo_caja_diario SERIAL PRIMARY KEY,
    id_caja INT NOT NULL,
    fecha DATE NOT NULL,
    saldo_inicial NUMERIC(10,2) DEFAULT 0,
    saldo_final NUMERIC(10,2) DEFAULT 0,
    total_ingresos NUMERIC(10,2) DEFAULT 0,
    total_egresos NUMERIC(10,2) DEFAULT 0,
    fecha_cierre TIMESTAMP DEFAULT NOW(),
    id_usuario_cierre INT NOT NULL,
    cerrado BOOLEAN DEFAULT FALSE
);
```

## 🔄 Mapeo Modelos ↔ Tablas PostgreSQL

| Modelo C# | Tabla PostgreSQL | Estado |
|------------|------------------|---------|
| `Usuario` | `usuario` | ✅ Correcto |
| `Producto` | `producto` | ✅ Correcto |
| `Caja` | `caja` | ✅ Correcto |
| `Ventum` | `venta` | ✅ Correcto |
| `DetalleVentum` | `detalle_venta` | ✅ Correcto |
| `MovimientoInventario` | `movimiento_inventario` | ✅ Recién corregido |
| `MovimientoCaja` | `movimiento_caja` | ✅ Correcto |
| `SaldoCajaDiario` | `saldo_caja_diario` | ✅ Correcto |

## 📝 Reglas de Nomenclatura

### ✅ **CORRECTO:**
- Tablas en minúsculas con guiones bajos: `movimiento_inventario`
- Columnas en minúsculas con guiones bajos: `id_producto`
- Tipos PostgreSQL: `NUMERIC(10,2)`, `TIMESTAMP`, `BOOLEAN`

### ❌ **INCORRECTO:**
- PascalCase: `MovimientoInventario`, `IdProducto`
- Mezcla de mayúsculas/minúsculas: `MovimientoInventario`
- Tipos SQL Server: `decimal(10,2)`, `datetime`

## 🔍 Verificación en PostgreSQL

Para verificar los nombres correctos en tu base de datos:

```sql
-- Ver todas las tablas
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
ORDER BY table_name;

-- Ver columnas de una tabla específica
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'movimiento_inventario' 
ORDER BY ordinal_position;
```

## ⚠️ Problemas Comunes

1. **Mayúsculas/Minúsculas**: PostgreSQL es case-sensitive
2. **Guiones bajos**: Usa `_` en lugar de camelCase
3. **Tipos de datos**: `NUMERIC` en lugar de `decimal`
4. **Timestamps**: `TIMESTAMP` en lugar de `datetime`

## 🚀 Solución Aplicada

Ya corregí el modelo `MovimientoInventario` para que coincida exactamente con la tabla `movimiento_inventario` de tu script SQL.
