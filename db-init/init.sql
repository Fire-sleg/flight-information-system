IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'FlightsDb')
BEGIN
    CREATE DATABASE FlightsDb;
END
GO

USE FlightsDb;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Flights')
BEGIN
    CREATE TABLE dbo.Flights (
        FlightNumber NVARCHAR(10) PRIMARY KEY,
        DepartureDateTime DATETIME2 NOT NULL,
        DepartureAirportCity NVARCHAR(100) NOT NULL,
        ArrivalAirportCity NVARCHAR(100) NOT NULL,
        DurationMinutes INT NOT NULL
    );
    INSERT INTO dbo.Flights
    (FlightNumber, DepartureDateTime, DepartureAirportCity, ArrivalAirportCity, DurationMinutes)
    VALUES
    ('FR101', DATEADD(HOUR, 5, GETDATE()), 'Dublin', 'London', 75),
    ('FR102', DATEADD(HOUR, 8, GETDATE()), 'Dublin', 'Paris', 110),
    ('LH203', DATEADD(DAY, 1, DATEADD(HOUR, 9, GETDATE())), 'Berlin', 'Rome', 125),
    ('LH204', DATEADD(DAY, 1, DATEADD(HOUR, 14, GETDATE())), 'Berlin', 'Madrid', 165),
    ('BA305', DATEADD(DAY, 2, DATEADD(HOUR, 7, GETDATE())), 'London', 'Dublin', 70),
    ('BA306', DATEADD(DAY, 2, DATEADD(HOUR, 18, GETDATE())), 'London', 'New York', 420),
    ('AF407', DATEADD(DAY, 3, DATEADD(HOUR, 6, GETDATE())), 'Paris', 'Amsterdam', 60),
    ('AF408', DATEADD(DAY, 3, DATEADD(HOUR, 16, GETDATE())), 'Paris', 'Vienna', 115),
    ('UA509', DATEADD(DAY, 4, DATEADD(HOUR, 10, GETDATE())), 'New York', 'Chicago', 150),
    ('UA510', DATEADD(DAY, 5, DATEADD(HOUR, 12, GETDATE())), 'Chicago', 'San Francisco', 270);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Flights_DepartureDateTime' AND object_id = OBJECT_ID('dbo.Flights'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Flights_DepartureDateTime
    ON dbo.Flights(DepartureDateTime)
    INCLUDE (DepartureAirportCity, ArrivalAirportCity, DurationMinutes);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Flights_DepartureCity_DateTime' AND object_id = OBJECT_ID('dbo.Flights'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Flights_DepartureCity_DateTime
    ON dbo.Flights(DepartureAirportCity, DepartureDateTime)
    INCLUDE (ArrivalAirportCity, DurationMinutes);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Flights_ArrivalCity_DateTime' AND object_id = OBJECT_ID('dbo.Flights'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Flights_ArrivalCity_DateTime
    ON dbo.Flights(ArrivalAirportCity, DepartureDateTime)
    INCLUDE (DepartureAirportCity, DurationMinutes);
END
GO

IF OBJECT_ID('dbo.AddFlight', 'P') IS NOT NULL DROP PROCEDURE dbo.AddFlight;
GO
CREATE PROCEDURE dbo.AddFlight
    @FlightNumber NVARCHAR(10),
    @DepartureDateTime DATETIME2,
    @DepartureAirportCity NVARCHAR(100),
    @ArrivalAirportCity NVARCHAR(100),
    @DurationMinutes INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @DepartureDateTime < GETDATE()
       OR @DepartureDateTime > DATEADD(DAY, 7, GETDATE())
    BEGIN
        THROW 50001, 'Flight date out of allowed range', 1;
    END

    INSERT INTO dbo.Flights
    VALUES
    (
        @FlightNumber,
        @DepartureDateTime,
        @DepartureAirportCity,
        @ArrivalAirportCity,
        @DurationMinutes
    );
END
GO


IF OBJECT_ID('dbo.GetFlightByNumber', 'P') IS NOT NULL DROP PROCEDURE dbo.GetFlightByNumber;
GO
CREATE PROCEDURE dbo.GetFlightByNumber
    @FlightNumber NVARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM dbo.Flights
    WHERE FlightNumber = @FlightNumber;
END
GO

IF OBJECT_ID('dbo.GetFlightsByDate', 'P') IS NOT NULL DROP PROCEDURE dbo.GetFlightsByDate;
GO
CREATE PROCEDURE dbo.GetFlightsByDate
    @Date DATE
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM dbo.Flights
    WHERE CAST(DepartureDateTime AS DATE) = @Date;
END
GO


IF OBJECT_ID('dbo.GetFlightsByDepartureCityAndDate', 'P') IS NOT NULL DROP PROCEDURE dbo.GetFlightsByDepartureCityAndDate;
GO
CREATE PROCEDURE dbo.GetFlightsByDepartureCityAndDate
    @City NVARCHAR(100),
    @Date DATE
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM dbo.Flights
    WHERE DepartureAirportCity = @City
      AND CAST(DepartureDateTime AS DATE) = @Date;
END
GO



IF OBJECT_ID('dbo.GetFlightsByArrivalCityAndDate', 'P') IS NOT NULL DROP PROCEDURE dbo.GetFlightsByArrivalCityAndDate;
GO
CREATE PROCEDURE dbo.GetFlightsByArrivalCityAndDate
    @City NVARCHAR(100),
    @Date DATE
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM dbo.Flights
    WHERE ArrivalAirportCity = @City
      AND CAST(DepartureDateTime AS DATE) = @Date;
END
GO


-- Створення нового логіна (користувача на рівні сервера)
IF NOT EXISTS (SELECT * FROM sys.sql_logins WHERE name = 'flight_api_user')
BEGIN
    CREATE LOGIN flight_api_user WITH PASSWORD = 'flight_api_user123!';
END
GO

IF NOT EXISTS (SELECT * FROM sys.sql_logins WHERE name = 'flight_api_admin')
BEGIN
    CREATE LOGIN flight_api_admin WITH PASSWORD = 'flight_api_admin123!';
END
GO


USE FlightsDb;
GO
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'flight_api_admin')
BEGIN
    CREATE USER flight_api_admin FOR LOGIN flight_api_admin;
    ALTER ROLE db_owner ADD MEMBER appuser;  
END
GO

IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'flight_api_user')
BEGIN
    CREATE USER flight_api_user FOR LOGIN flight_api_user;
    DENY SELECT, INSERT, UPDATE, DELETE
    ON dbo.Flights
    TO flight_api_user
    GO
    GRANT EXECUTE TO flight_api_user
    GO
END
GO


