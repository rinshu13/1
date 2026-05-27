/*
  UNIVERSAL EXAM SQL FILE
  DB: MS SQL Server

  Project/DB name used in this ready template: Magazin
*/

IF DB_ID(N'Magazin') IS NULL
BEGIN
    CREATE DATABASE Magazin;
END
GO

USE Magazin;
GO

IF OBJECT_ID(N'dbo.vLogin', N'V') IS NOT NULL DROP VIEW dbo.vLogin;
IF OBJECT_ID(N'dbo.vDocumentList', N'V') IS NOT NULL DROP VIEW dbo.vDocumentList;
IF OBJECT_ID(N'dbo.vMainObjectList', N'V') IS NOT NULL DROP VIEW dbo.vMainObjectList;
GO

IF OBJECT_ID(N'dbo.DocumentItem', N'U') IS NOT NULL DROP TABLE dbo.DocumentItem;
IF OBJECT_ID(N'dbo.Document', N'U') IS NOT NULL DROP TABLE dbo.Document;
IF OBJECT_ID(N'dbo.MainObjectPhoto', N'U') IS NOT NULL DROP TABLE dbo.MainObjectPhoto;
IF OBJECT_ID(N'dbo.MainObject', N'U') IS NOT NULL DROP TABLE dbo.MainObject;
IF OBJECT_ID(N'dbo.Client', N'U') IS NOT NULL DROP TABLE dbo.Client;
IF OBJECT_ID(N'dbo.Employee', N'U') IS NOT NULL DROP TABLE dbo.Employee;
IF OBJECT_ID(N'dbo.Category', N'U') IS NOT NULL DROP TABLE dbo.Category;
IF OBJECT_ID(N'dbo.[Role]', N'U') IS NOT NULL DROP TABLE dbo.[Role];
GO

CREATE TABLE dbo.[Role]
(
    ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Role PRIMARY KEY,
    Title NVARCHAR(100) NOT NULL
);
GO

CREATE TABLE dbo.Employee
(
    ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Employee PRIMARY KEY,
    LastName NVARCHAR(50) NOT NULL,
    FirstName NVARCHAR(50) NOT NULL,
    Patronymic NVARCHAR(50) NULL,
    Login NVARCHAR(50) NOT NULL,
    [Password] NVARCHAR(50) NOT NULL,
    RoleID INT NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Employee_IsActive DEFAULT 1,
    CONSTRAINT FK_Employee_Role FOREIGN KEY (RoleID) REFERENCES dbo.[Role](ID)
);
GO

CREATE TABLE dbo.Category
(
    ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Category PRIMARY KEY,
    Title NVARCHAR(100) NOT NULL
);
GO

CREATE TABLE dbo.MainObject
(
    ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MainObject PRIMARY KEY,
    Article NVARCHAR(50) NULL,
    Title NVARCHAR(150) NOT NULL,
    CategoryID INT NULL,
    Cost MONEY NOT NULL,
    Discount FLOAT NULL,
    Quantity INT NULL,
    Description NVARCHAR(MAX) NULL,
    MainImagePath NVARCHAR(1000) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_MainObject_IsActive DEFAULT 1,
    CONSTRAINT FK_MainObject_Category FOREIGN KEY (CategoryID) REFERENCES dbo.Category(ID)
);
GO

CREATE TABLE dbo.MainObjectPhoto
(
    ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MainObjectPhoto PRIMARY KEY,
    MainObjectID INT NOT NULL,
    PhotoPath NVARCHAR(1000) NOT NULL,
    CONSTRAINT FK_MainObjectPhoto_MainObject FOREIGN KEY (MainObjectID) REFERENCES dbo.MainObject(ID)
);
GO

CREATE TABLE dbo.Client
(
    ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Client PRIMARY KEY,
    LastName NVARCHAR(50) NOT NULL,
    FirstName NVARCHAR(50) NOT NULL,
    Patronymic NVARCHAR(50) NULL,
    Birthday DATE NULL,
    Phone NVARCHAR(20) NULL,
    Email NVARCHAR(255) NULL,
    RegistrationDate DATETIME NOT NULL CONSTRAINT DF_Client_RegistrationDate DEFAULT GETDATE(),
    PhotoPath NVARCHAR(1000) NULL
);
GO

CREATE TABLE dbo.Document
(
    ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Document PRIMARY KEY,
    ClientID INT NULL,
    EmployeeID INT NULL,
    CreateDate DATETIME NOT NULL CONSTRAINT DF_Document_CreateDate DEFAULT GETDATE(),
    StatusTitle NVARCHAR(50) NOT NULL CONSTRAINT DF_Document_StatusTitle DEFAULT N'Новый',
    Comment NVARCHAR(MAX) NULL,
    CONSTRAINT FK_Document_Client FOREIGN KEY (ClientID) REFERENCES dbo.Client(ID),
    CONSTRAINT FK_Document_Employee FOREIGN KEY (EmployeeID) REFERENCES dbo.Employee(ID)
);
GO

CREATE TABLE dbo.DocumentItem
(
    ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DocumentItem PRIMARY KEY,
    DocumentID INT NOT NULL,
    MainObjectID INT NOT NULL,
    Quantity INT NOT NULL CONSTRAINT DF_DocumentItem_Quantity DEFAULT 1,
    Cost MONEY NOT NULL,
    CONSTRAINT FK_DocumentItem_Document FOREIGN KEY (DocumentID) REFERENCES dbo.Document(ID),
    CONSTRAINT FK_DocumentItem_MainObject FOREIGN KEY (MainObjectID) REFERENCES dbo.MainObject(ID)
);
GO

INSERT INTO dbo.[Role] (Title)
VALUES (N'Администратор'), (N'Пользователь');

INSERT INTO dbo.Employee (LastName, FirstName, Login, [Password], RoleID)
VALUES (N'Админ', N'Системный', N'admin', N'admin', 1);

INSERT INTO dbo.Category (Title)
VALUES (N'Категория 1'), (N'Категория 2');

INSERT INTO dbo.MainObject (Article, Title, CategoryID, Cost, Discount, Quantity, Description)
VALUES
    (N'A001', N'Позиция 1', 1, 1000, 0.10, 5, N'Описание'),
    (N'A002', N'Позиция 2', 2, 1500, 0, 8, N'Описание');
GO

CREATE VIEW dbo.vMainObjectList
AS
SELECT
    mo.ID,
    mo.Article,
    mo.Title,
    mo.CategoryID,
    ISNULL(c.Title, N'Без категории') AS CategoryTitle,
    mo.Cost,
    ISNULL(mo.Discount, 0) AS Discount,
    CAST(mo.Cost * (1 - ISNULL(mo.Discount, 0)) AS MONEY) AS CostWithDiscount,
    mo.Quantity,
    mo.Description,
    mo.MainImagePath,
    mo.IsActive
FROM dbo.MainObject AS mo
LEFT JOIN dbo.Category AS c ON c.ID = mo.CategoryID;
GO

CREATE VIEW dbo.vDocumentList
AS
SELECT
    d.ID,
    d.CreateDate,
    d.StatusTitle,
    d.Comment,
    CONCAT(cl.LastName, N' ', cl.FirstName, N' ', ISNULL(cl.Patronymic, N'')) AS ClientFullName,
    CONCAT(e.LastName, N' ', e.FirstName, N' ', ISNULL(e.Patronymic, N'')) AS EmployeeFullName,
    COUNT(di.ID) AS PositionCount,
    SUM(di.Quantity) AS TotalQuantity,
    SUM(di.Quantity * di.Cost) AS TotalCost
FROM dbo.Document AS d
LEFT JOIN dbo.Client AS cl ON cl.ID = d.ClientID
LEFT JOIN dbo.Employee AS e ON e.ID = d.EmployeeID
LEFT JOIN dbo.DocumentItem AS di ON di.DocumentID = d.ID
GROUP BY
    d.ID,
    d.CreateDate,
    d.StatusTitle,
    d.Comment,
    cl.LastName,
    cl.FirstName,
    cl.Patronymic,
    e.LastName,
    e.FirstName,
    e.Patronymic;
GO

CREATE VIEW dbo.vLogin
AS
SELECT
    e.ID,
    e.Login,
    e.[Password],
    e.LastName,
    e.FirstName,
    e.Patronymic,
    r.Title AS RoleTitle,
    e.IsActive
FROM dbo.Employee AS e
INNER JOIN dbo.[Role] AS r ON r.ID = e.RoleID
WHERE e.IsActive = 1;
GO
