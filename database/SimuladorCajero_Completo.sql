/* ============================================================
   SIMULADOR DE CAJERO AUTOMÁTICO
   Base de datos: SimuladorCajeroDB
   Motor: SQL Server
   ============================================================ */

USE master;
GO

/* ============================================================
   1. ELIMINAR LA BASE DE DATOS SI YA EXISTE
   Permite ejecutar el script nuevamente desde cero.
   ============================================================ */

IF DB_ID(N'SimuladorCajeroDB') IS NOT NULL
BEGIN
    ALTER DATABASE SimuladorCajeroDB
    SET SINGLE_USER
    WITH ROLLBACK IMMEDIATE;

    DROP DATABASE SimuladorCajeroDB;
END;
GO

/* ============================================================
   2. CREAR LA BASE DE DATOS
   ============================================================ */

CREATE DATABASE SimuladorCajeroDB;
GO

USE SimuladorCajeroDB;
GO

/* Opciones necesarias para índices filtrados */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/* ============================================================
   3. TABLA USUARIOS
   Guarda la información de los clientes.
   ============================================================ */

CREATE TABLE dbo.Usuarios
(
    IdUsuario INT IDENTITY(1,1) NOT NULL,

    Nombre NVARCHAR(100) NOT NULL,
    ApellidoPaterno NVARCHAR(100) NOT NULL,
    ApellidoMaterno NVARCHAR(100) NULL,

    Correo NVARCHAR(150) NOT NULL,

    Activo BIT NOT NULL
        CONSTRAINT DF_Usuarios_Activo DEFAULT (1),

    FechaRegistro DATETIME2(0) NOT NULL
        CONSTRAINT DF_Usuarios_FechaRegistro DEFAULT (SYSDATETIME()),

    CONSTRAINT PK_Usuarios
        PRIMARY KEY (IdUsuario),

    CONSTRAINT UQ_Usuarios_Correo
        UNIQUE (Correo)
);
GO

/* ============================================================
   4. TABLA CUENTAS
   Guarda el número de cuenta y el saldo disponible.
   ============================================================ */

CREATE TABLE dbo.Cuentas
(
    IdCuenta INT IDENTITY(1,1) NOT NULL,

    IdUsuario INT NOT NULL,

    NumeroCuenta VARCHAR(20) NOT NULL,

    Saldo DECIMAL(18,2) NOT NULL
        CONSTRAINT DF_Cuentas_Saldo DEFAULT (0),

    Activa BIT NOT NULL
        CONSTRAINT DF_Cuentas_Activa DEFAULT (1),

    FechaCreacion DATETIME2(0) NOT NULL
        CONSTRAINT DF_Cuentas_FechaCreacion DEFAULT (SYSDATETIME()),

    CONSTRAINT PK_Cuentas
        PRIMARY KEY (IdCuenta),

    CONSTRAINT UQ_Cuentas_NumeroCuenta
        UNIQUE (NumeroCuenta),

    CONSTRAINT CK_Cuentas_Saldo
        CHECK (Saldo >= 0),

    CONSTRAINT FK_Cuentas_Usuarios
        FOREIGN KEY (IdUsuario)
        REFERENCES dbo.Usuarios(IdUsuario)
);
GO

/* ============================================================
   5. TABLA TARJETAS
   Almacena las tarjetas asociadas a las cuentas.
   El NIP se guardará como hash, nunca como texto directo.
   ============================================================ */

CREATE TABLE dbo.Tarjetas
(
    IdTarjeta INT IDENTITY(1,1) NOT NULL,

    IdCuenta INT NOT NULL,

    NumeroTarjeta CHAR(16) NOT NULL,

    NipHash NVARCHAR(255) NOT NULL,

    Bloqueada BIT NOT NULL
        CONSTRAINT DF_Tarjetas_Bloqueada DEFAULT (0),

    IntentosFallidos TINYINT NOT NULL
        CONSTRAINT DF_Tarjetas_IntentosFallidos DEFAULT (0),

    FechaExpiracion DATE NOT NULL,

    Activa BIT NOT NULL
        CONSTRAINT DF_Tarjetas_Activa DEFAULT (1),

    FechaCreacion DATETIME2(0) NOT NULL
        CONSTRAINT DF_Tarjetas_FechaCreacion DEFAULT (SYSDATETIME()),

    CONSTRAINT PK_Tarjetas
        PRIMARY KEY (IdTarjeta),

    CONSTRAINT UQ_Tarjetas_NumeroTarjeta
        UNIQUE (NumeroTarjeta),

    CONSTRAINT CK_Tarjetas_NumeroTarjeta
        CHECK
        (
            LEN(NumeroTarjeta) = 16
            AND NumeroTarjeta NOT LIKE '%[^0-9]%'
        ),

    CONSTRAINT CK_Tarjetas_IntentosFallidos
        CHECK (IntentosFallidos BETWEEN 0 AND 3),

    CONSTRAINT FK_Tarjetas_Cuentas
        FOREIGN KEY (IdCuenta)
        REFERENCES dbo.Cuentas(IdCuenta)
);
GO

/* ============================================================
   6. TABLA TRANSACCIONES
   Registra depósitos, retiros y reversiones.
   ============================================================ */

CREATE TABLE dbo.Transacciones
(
    IdTransaccion BIGINT IDENTITY(1,1) NOT NULL,

    IdCuenta INT NOT NULL,

    Tipo VARCHAR(10) NOT NULL,

    Monto DECIMAL(18,2) NOT NULL,

    SaldoAnterior DECIMAL(18,2) NOT NULL,

    SaldoPosterior DECIMAL(18,2) NOT NULL,

    Estado VARCHAR(10) NOT NULL
        CONSTRAINT DF_Transacciones_Estado DEFAULT ('APLICADA'),

    FechaTransaccion DATETIME2(0) NOT NULL
        CONSTRAINT DF_Transacciones_Fecha DEFAULT (SYSDATETIME()),

    IdTransaccionOriginal BIGINT NULL,

    MotivoReversion NVARCHAR(250) NULL,

    Referencia UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_Transacciones_Referencia DEFAULT (NEWID()),

    CONSTRAINT PK_Transacciones
        PRIMARY KEY (IdTransaccion),

    CONSTRAINT CK_Transacciones_Tipo
        CHECK (Tipo IN ('DEPOSITO', 'RETIRO', 'REVERSO')),

    CONSTRAINT CK_Transacciones_Monto
        CHECK (Monto > 0),

    CONSTRAINT CK_Transacciones_SaldoAnterior
        CHECK (SaldoAnterior >= 0),

    CONSTRAINT CK_Transacciones_SaldoPosterior
        CHECK (SaldoPosterior >= 0),

    CONSTRAINT CK_Transacciones_Estado
        CHECK (Estado IN ('APLICADA', 'RECHAZADA', 'REVERTIDA')),

    CONSTRAINT UQ_Transacciones_Referencia
        UNIQUE (Referencia),

    CONSTRAINT FK_Transacciones_Cuentas
        FOREIGN KEY (IdCuenta)
        REFERENCES dbo.Cuentas(IdCuenta),

    CONSTRAINT FK_Transacciones_TransaccionOriginal
        FOREIGN KEY (IdTransaccionOriginal)
        REFERENCES dbo.Transacciones(IdTransaccion)
);
GO

/* ============================================================
   7. ÍNDICES
   Mejoran la velocidad de las búsquedas.
   ============================================================ */

CREATE INDEX IX_Cuentas_IdUsuario
ON dbo.Cuentas(IdUsuario);
GO

CREATE INDEX IX_Tarjetas_IdCuenta
ON dbo.Tarjetas(IdCuenta);
GO

CREATE INDEX IX_Transacciones_IdCuenta_Fecha
ON dbo.Transacciones(IdCuenta, FechaTransaccion DESC);
GO

CREATE UNIQUE INDEX UX_Transacciones_IdTransaccionOriginal
ON dbo.Transacciones(IdTransaccionOriginal)
WHERE IdTransaccionOriginal IS NOT NULL;
GO

/* ============================================================
   8. PROCEDIMIENTO: CONSULTAR SALDO
   ============================================================ */

CREATE OR ALTER PROCEDURE dbo.sp_ConsultarSaldo
    @IdCuenta INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.Cuentas
        WHERE IdCuenta = @IdCuenta
          AND Activa = 1
    )
    BEGIN
        THROW 50001, 'La cuenta no existe o se encuentra inactiva.', 1;
    END;

    SELECT
        IdCuenta,
        NumeroCuenta,
        Saldo,
        Activa
    FROM dbo.Cuentas
    WHERE IdCuenta = @IdCuenta;
END;
GO

/* ============================================================
   9. PROCEDIMIENTO: REGISTRAR DEPÓSITO
   ============================================================ */

CREATE OR ALTER PROCEDURE dbo.sp_RegistrarDeposito
    @IdCuenta INT,
    @Monto DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Monto IS NULL OR @Monto <= 0
    BEGIN
        THROW 50002, 'El monto del depósito debe ser mayor que cero.', 1;
    END;

    DECLARE @SaldoAnterior DECIMAL(18,2);
    DECLARE @SaldoPosterior DECIMAL(18,2);
    DECLARE @IdTransaccion BIGINT;

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @SaldoAnterior = Saldo
        FROM dbo.Cuentas WITH (UPDLOCK, HOLDLOCK)
        WHERE IdCuenta = @IdCuenta
          AND Activa = 1;

        IF @SaldoAnterior IS NULL
        BEGIN
            THROW 50001, 'La cuenta no existe o se encuentra inactiva.', 1;
        END;

        SET @SaldoPosterior = @SaldoAnterior + @Monto;

        UPDATE dbo.Cuentas
        SET Saldo = @SaldoPosterior
        WHERE IdCuenta = @IdCuenta;

        INSERT INTO dbo.Transacciones
        (
            IdCuenta,
            Tipo,
            Monto,
            SaldoAnterior,
            SaldoPosterior,
            Estado
        )
        VALUES
        (
            @IdCuenta,
            'DEPOSITO',
            @Monto,
            @SaldoAnterior,
            @SaldoPosterior,
            'APLICADA'
        );

        SET @IdTransaccion = CONVERT(BIGINT, SCOPE_IDENTITY());

        COMMIT TRANSACTION;

        SELECT
            @IdTransaccion AS IdTransaccion,
            'DEPOSITO' AS Tipo,
            @Monto AS Monto,
            @SaldoAnterior AS SaldoAnterior,
            @SaldoPosterior AS SaldoPosterior,
            'Depósito realizado correctamente.' AS Mensaje;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH;
END;
GO

/* ============================================================
   10. PROCEDIMIENTO: REGISTRAR RETIRO
   Regla principal: no permite retirar más que el saldo.
   ============================================================ */

CREATE OR ALTER PROCEDURE dbo.sp_RegistrarRetiro
    @IdCuenta INT,
    @Monto DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Monto IS NULL OR @Monto <= 0
    BEGIN
        THROW 50003, 'El monto del retiro debe ser mayor que cero.', 1;
    END;

    DECLARE @SaldoAnterior DECIMAL(18,2);
    DECLARE @SaldoPosterior DECIMAL(18,2);
    DECLARE @IdTransaccion BIGINT;

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @SaldoAnterior = Saldo
        FROM dbo.Cuentas WITH (UPDLOCK, HOLDLOCK)
        WHERE IdCuenta = @IdCuenta
          AND Activa = 1;

        IF @SaldoAnterior IS NULL
        BEGIN
            THROW 50001, 'La cuenta no existe o se encuentra inactiva.', 1;
        END;

        IF @SaldoAnterior < @Monto
        BEGIN
            INSERT INTO dbo.Transacciones
            (
                IdCuenta,
                Tipo,
                Monto,
                SaldoAnterior,
                SaldoPosterior,
                Estado
            )
            VALUES
            (
                @IdCuenta,
                'RETIRO',
                @Monto,
                @SaldoAnterior,
                @SaldoAnterior,
                'RECHAZADA'
            );

            COMMIT TRANSACTION;

            THROW 50004,
                'Saldo insuficiente para realizar la operación.',
                1;
        END;

        SET @SaldoPosterior = @SaldoAnterior - @Monto;

        UPDATE dbo.Cuentas
        SET Saldo = @SaldoPosterior
        WHERE IdCuenta = @IdCuenta;

        INSERT INTO dbo.Transacciones
        (
            IdCuenta,
            Tipo,
            Monto,
            SaldoAnterior,
            SaldoPosterior,
            Estado
        )
        VALUES
        (
            @IdCuenta,
            'RETIRO',
            @Monto,
            @SaldoAnterior,
            @SaldoPosterior,
            'APLICADA'
        );

        SET @IdTransaccion = CONVERT(BIGINT, SCOPE_IDENTITY());

        COMMIT TRANSACTION;

        SELECT
            @IdTransaccion AS IdTransaccion,
            'RETIRO' AS Tipo,
            @Monto AS Monto,
            @SaldoAnterior AS SaldoAnterior,
            @SaldoPosterior AS SaldoPosterior,
            'Retiro realizado correctamente.' AS Mensaje;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH;
END;
GO

/* ============================================================
   11. PROCEDIMIENTO: CAMBIAR NIP
   Recibe el nuevo hash generado por la aplicación.
   ============================================================ */

CREATE OR ALTER PROCEDURE dbo.sp_CambiarNip
    @IdTarjeta INT,
    @NuevoNipHash NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    IF @NuevoNipHash IS NULL OR LEN(LTRIM(RTRIM(@NuevoNipHash))) = 0
    BEGIN
        THROW 50005, 'El nuevo hash del NIP es obligatorio.', 1;
    END;

    UPDATE dbo.Tarjetas
    SET NipHash = @NuevoNipHash
    WHERE IdTarjeta = @IdTarjeta
      AND Activa = 1
      AND Bloqueada = 0;

    IF @@ROWCOUNT = 0
    BEGIN
        THROW 50006,
            'La tarjeta no existe, está inactiva o está bloqueada.',
            1;
    END;

    SELECT
        IdTarjeta,
        NumeroTarjeta,
        'NIP actualizado correctamente.' AS Mensaje
    FROM dbo.Tarjetas
    WHERE IdTarjeta = @IdTarjeta;
END;
GO

/* ============================================================
   12. PROCEDIMIENTO: REVERTIR TRANSACCIÓN
   No elimina la transacción; conserva el historial.
   ============================================================ */

CREATE OR ALTER PROCEDURE dbo.sp_RevertirTransaccion
    @IdTransaccion BIGINT,
    @Motivo NVARCHAR(250)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @IdCuenta INT;
    DECLARE @TipoOriginal VARCHAR(10);
    DECLARE @Monto DECIMAL(18,2);
    DECLARE @SaldoAnterior DECIMAL(18,2);
    DECLARE @SaldoPosterior DECIMAL(18,2);
    DECLARE @IdReversion BIGINT;

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT
            @IdCuenta = IdCuenta,
            @TipoOriginal = Tipo,
            @Monto = Monto
        FROM dbo.Transacciones WITH (UPDLOCK, HOLDLOCK)
        WHERE IdTransaccion = @IdTransaccion
          AND Estado = 'APLICADA'
          AND Tipo IN ('DEPOSITO', 'RETIRO');

        IF @IdCuenta IS NULL
        BEGIN
            THROW 50007,
                'La transacción no existe, no está aplicada o no puede revertirse.',
                1;
        END;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.Transacciones
            WHERE IdTransaccionOriginal = @IdTransaccion
        )
        BEGIN
            THROW 50008, 'La transacción ya fue revertida.', 1;
        END;

        SELECT @SaldoAnterior = Saldo
        FROM dbo.Cuentas WITH (UPDLOCK, HOLDLOCK)
        WHERE IdCuenta = @IdCuenta
          AND Activa = 1;

        IF @SaldoAnterior IS NULL
        BEGIN
            THROW 50001, 'La cuenta no existe o se encuentra inactiva.', 1;
        END;

        IF @TipoOriginal = 'DEPOSITO'
        BEGIN
            IF @SaldoAnterior < @Monto
            BEGIN
                THROW 50009,
                    'No existe saldo suficiente para revertir el depósito.',
                    1;
            END;

            SET @SaldoPosterior = @SaldoAnterior - @Monto;
        END;
        ELSE
        BEGIN
            SET @SaldoPosterior = @SaldoAnterior + @Monto;
        END;

        UPDATE dbo.Cuentas
        SET Saldo = @SaldoPosterior
        WHERE IdCuenta = @IdCuenta;

        UPDATE dbo.Transacciones
        SET
            Estado = 'REVERTIDA',
            MotivoReversion = @Motivo
        WHERE IdTransaccion = @IdTransaccion;

        INSERT INTO dbo.Transacciones
        (
            IdCuenta,
            Tipo,
            Monto,
            SaldoAnterior,
            SaldoPosterior,
            Estado,
            IdTransaccionOriginal,
            MotivoReversion
        )
        VALUES
        (
            @IdCuenta,
            'REVERSO',
            @Monto,
            @SaldoAnterior,
            @SaldoPosterior,
            'APLICADA',
            @IdTransaccion,
            @Motivo
        );

        SET @IdReversion = CONVERT(BIGINT, SCOPE_IDENTITY());

        COMMIT TRANSACTION;

        SELECT
            @IdReversion AS IdReversion,
            @IdTransaccion AS IdTransaccionOriginal,
            @Monto AS Monto,
            @SaldoAnterior AS SaldoAnterior,
            @SaldoPosterior AS SaldoPosterior,
            'Transacción revertida correctamente.' AS Mensaje;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH;
END;
GO

/* ============================================================
   13. DATOS DE PRUEBA
   ============================================================ */

INSERT INTO dbo.Usuarios
(
    Nombre,
    ApellidoPaterno,
    ApellidoMaterno,
    Correo
)
VALUES
(
    N'Bernardo',
    N'Martínez',
    NULL,
    N'bernardo@cajero.com'
),
(
    N'Ana',
    N'López',
    N'García',
    N'ana@cajero.com'
);
GO

/* Obtener los usuarios creados */
DECLARE @IdUsuarioBernardo INT;
DECLARE @IdUsuarioAna INT;

SELECT @IdUsuarioBernardo = IdUsuario
FROM dbo.Usuarios
WHERE Correo = N'bernardo@cajero.com';

SELECT @IdUsuarioAna = IdUsuario
FROM dbo.Usuarios
WHERE Correo = N'ana@cajero.com';

/* Crear cuentas con saldo inicial */
INSERT INTO dbo.Cuentas
(
    IdUsuario,
    NumeroCuenta,
    Saldo
)
VALUES
(
    @IdUsuarioBernardo,
    '1000000001',
    5000.00
),
(
    @IdUsuarioAna,
    '1000000002',
    2500.00
);
GO

/* Obtener las cuentas creadas */
DECLARE @IdCuentaBernardo INT;
DECLARE @IdCuentaAna INT;

SELECT @IdCuentaBernardo = IdCuenta
FROM dbo.Cuentas
WHERE NumeroCuenta = '1000000001';

SELECT @IdCuentaAna = IdCuenta
FROM dbo.Cuentas
WHERE NumeroCuenta = '1000000002';

/*
   Los valores de NipHash son temporales.
   Posteriormente la API generará hashes reales con BCrypt.
*/
INSERT INTO dbo.Tarjetas
(
    IdCuenta,
    NumeroTarjeta,
    NipHash,
    FechaExpiracion
)
VALUES
(
    @IdCuentaBernardo,
    '4000000000000001',
    N'PENDIENTE_BCRYPT_1234',
    '2030-12-31'
),
(
    @IdCuentaAna,
    '4000000000000002',
    N'PENDIENTE_BCRYPT_5678',
    '2030-12-31'
);
GO

PRINT 'Datos de prueba insertados correctamente.';
GO

PRINT 'Base de datos, tablas, índices, procedimientos y datos de prueba creados correctamente.';
GO