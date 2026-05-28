/*
  Единый SQL-скрипт для демонстрационного экзамена.
  СУБД: Microsoft SQL Server.
*/

IF DB_ID(N'Magazin') IS NULL
    CREATE DATABASE Magazin;
GO
USE Magazin;
GO

IF OBJECT_ID(N'dbo.vProductList', N'V') IS NOT NULL DROP VIEW dbo.vProductList;
IF OBJECT_ID(N'dbo.vOrderList', N'V') IS NOT NULL DROP VIEW dbo.vOrderList;
IF OBJECT_ID(N'dbo.vLogin', N'V') IS NOT NULL DROP VIEW dbo.vLogin;
GO
IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NOT NULL DROP TABLE dbo.OrderItems;
IF OBJECT_ID(N'dbo.Orders', N'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID(N'dbo.Products', N'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID(N'dbo.Clients', N'U') IS NOT NULL DROP TABLE dbo.Clients;
IF OBJECT_ID(N'dbo.PickupPoints', N'U') IS NOT NULL DROP TABLE dbo.PickupPoints;
IF OBJECT_ID(N'dbo.OrderStatuses', N'U') IS NOT NULL DROP TABLE dbo.OrderStatuses;
IF OBJECT_ID(N'dbo.Roles', N'U') IS NOT NULL DROP TABLE dbo.Roles;
IF OBJECT_ID(N'dbo.Suppliers', N'U') IS NOT NULL DROP TABLE dbo.Suppliers;
IF OBJECT_ID(N'dbo.Manufacturers', N'U') IS NOT NULL DROP TABLE dbo.Manufacturers;
IF OBJECT_ID(N'dbo.Categories', N'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID(N'dbo.Units', N'U') IS NOT NULL DROP TABLE dbo.Units;
GO
CREATE TABLE dbo.Roles (
    ID INT IDENTITY(1,1) CONSTRAINT PK_Roles PRIMARY KEY,
    Title NVARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE dbo.Users (
    ID INT IDENTITY(1,1) CONSTRAINT PK_Users PRIMARY KEY,
    RoleID INT NOT NULL,
    FullName NVARCHAR(150) NOT NULL,
    Login NVARCHAR(100) NOT NULL UNIQUE,
    [Password] NVARCHAR(100) NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleID) REFERENCES dbo.Roles(ID)
);

CREATE TABLE dbo.Suppliers (
    ID INT IDENTITY(1,1) CONSTRAINT PK_Suppliers PRIMARY KEY,
    Title NVARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE dbo.Manufacturers (
    ID INT IDENTITY(1,1) CONSTRAINT PK_Manufacturers PRIMARY KEY,
    Title NVARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE dbo.Categories (
    ID INT IDENTITY(1,1) CONSTRAINT PK_Categories PRIMARY KEY,
    Title NVARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE dbo.Units (
    ID INT IDENTITY(1,1) CONSTRAINT PK_Units PRIMARY KEY,
    Title NVARCHAR(30) NOT NULL UNIQUE
);

CREATE TABLE dbo.Products (
    ID INT IDENTITY(1,1) CONSTRAINT PK_Products PRIMARY KEY,
    Article NVARCHAR(20) NOT NULL UNIQUE,
    Title NVARCHAR(500) NOT NULL,
    UnitID INT NOT NULL,
    Cost DECIMAL(10,2) NOT NULL CONSTRAINT CK_Products_Cost CHECK (Cost >= 0),
    SupplierID INT NOT NULL,
    ManufacturerID INT NOT NULL,
    CategoryID INT NOT NULL,
    Discount INT NOT NULL CONSTRAINT DF_Products_Discount DEFAULT 0 CONSTRAINT CK_Products_Discount CHECK (Discount >= 0 AND Discount <= 100),
    Quantity INT NOT NULL CONSTRAINT DF_Products_Quantity DEFAULT 0 CONSTRAINT CK_Products_Quantity CHECK (Quantity >= 0),
    Description NVARCHAR(MAX) NULL,
    PhotoPath NVARCHAR(260) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT 1,
    CONSTRAINT FK_Products_Units FOREIGN KEY (UnitID) REFERENCES dbo.Units(ID),
    CONSTRAINT FK_Products_Suppliers FOREIGN KEY (SupplierID) REFERENCES dbo.Suppliers(ID),
    CONSTRAINT FK_Products_Manufacturers FOREIGN KEY (ManufacturerID) REFERENCES dbo.Manufacturers(ID),
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryID) REFERENCES dbo.Categories(ID)
);

CREATE TABLE dbo.PickupPoints (
    ID INT IDENTITY(1,1) CONSTRAINT PK_PickupPoints PRIMARY KEY,
    Address NVARCHAR(300) NOT NULL
);

CREATE TABLE dbo.Clients (
    ID INT IDENTITY(1,1) CONSTRAINT PK_Clients PRIMARY KEY,
    FullName NVARCHAR(150) NOT NULL UNIQUE
);

CREATE TABLE dbo.OrderStatuses (
    ID INT IDENTITY(1,1) CONSTRAINT PK_OrderStatuses PRIMARY KEY,
    Title NVARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE dbo.Orders (
    ID INT IDENTITY(1,1) CONSTRAINT PK_Orders PRIMARY KEY,
    OrderDate DATE NOT NULL,
    DeliveryDate DATE NOT NULL,
    PickupPointID INT NOT NULL,
    ClientID INT NULL,
    ReceiveCode INT NOT NULL,
    StatusID INT NOT NULL,
    CONSTRAINT FK_Orders_PickupPoints FOREIGN KEY (PickupPointID) REFERENCES dbo.PickupPoints(ID),
    CONSTRAINT FK_Orders_Clients FOREIGN KEY (ClientID) REFERENCES dbo.Clients(ID),
    CONSTRAINT FK_Orders_OrderStatuses FOREIGN KEY (StatusID) REFERENCES dbo.OrderStatuses(ID)
);

CREATE TABLE dbo.OrderItems (
    ID INT IDENTITY(1,1) CONSTRAINT PK_OrderItems PRIMARY KEY,
    OrderID INT NOT NULL,
    ProductID INT NOT NULL,
    Quantity INT NOT NULL CONSTRAINT CK_OrderItems_Quantity CHECK (Quantity > 0),
    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderID) REFERENCES dbo.Orders(ID),
    CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductID) REFERENCES dbo.Products(ID)
);
GO

INSERT INTO dbo.Roles (Title) VALUES (N'Администратор'), (N'Менеджер'), (N'Клиент');
GO

INSERT INTO dbo.Users (RoleID, FullName, Login, [Password]) VALUES
(1, N'Ворсин Петр Евгеньевич', N'94d5ous@gmail.com', N'uzWC67'),
(1, N'Старикова Елена Павловна', N'uth4iz@mail.com', N'2L6KZG'),
(1, N'Одинцов Серафим Артёмович', N'yzls62@outlook.com', N'JlFRCZ'),
(2, N'Михайлюк Анна Вячеславовна', N'1diph5e@tutanota.com', N'8ntwUp'),
(2, N'Ситдикова Елена Анатольевна', N'tjde7c@yahoo.com', N'YOyhfR'),
(2, N'Никифорова Весения Николаевна', N'wpmrc3do@tutanota.com', N'RSbvHv'),
(3, N'Степанов Михаил Артёмович', N'5d4zbu@tutanota.com', N'rwVDh9'),
(3, N'Ворсин Петр Евгеньевич', N'ptec8ym@yahoo.com', N'LdNyos'),
(3, N'Старикова Елена Павловна', N'1qz4kw@mail.com', N'gynQMT'),
(3, N'Сазонов Руслан Германович', N'4np6se@mail.com', N'AtnDjr'),
(3, N'Иванов Иван Иванович', N'client', N'client');
GO

INSERT INTO dbo.Suppliers (Title) VALUES
(N'CHILITOY'),
(N'Knauf'),
(N'Pikeshop'),
(N'Playbig'),
(N'Vinylon');
GO
INSERT INTO dbo.Manufacturers (Title) VALUES
(N'ABSпластик'),
(N'BambiniFelici'),
(N'Junion');
GO
INSERT INTO dbo.Categories (Title) VALUES
(N'Детский музыкальный инструмент'),
(N'Игровой набор'),
(N'Конструктор'),
(N'Машинка');
GO
INSERT INTO dbo.Units (Title) VALUES
(N'шт.');
GO
INSERT INTO dbo.OrderStatuses (Title) VALUES
(N'Завершен'),
(N'Новый');
GO
INSERT INTO dbo.PickupPoints (Address) VALUES
(N'420151, г. Лесной, ул. Вишневая, 32'),
(N'125061, г. Лесной, ул. Подгорная, 8'),
(N'630370, г. Лесной, ул. Шоссейная, 24'),
(N'400562, г. Лесной, ул. Зеленая, 32'),
(N'614510, г. Лесной, ул. Маяковского, 47'),
(N'410542, г. Лесной, ул. Светлая, 46'),
(N'620839, г. Лесной, ул. Цветочная, 8'),
(N'443890, г. Лесной, ул. Коммунистическая, 1'),
(N'603379, г. Лесной, ул. Спортивная, 46'),
(N'603721, г. Лесной, ул. Гоголя, 41'),
(N'410172, г. Лесной, ул. Северная, 13'),
(N'614611, г. Лесной, ул. Молодежная, 50'),
(N'454311, г.Лесной, ул. Новая, 19'),
(N'660007, г.Лесной, ул. Октябрьская, 19'),
(N'603036, г. Лесной, ул. Садовая, 4'),
(N'394060, г.Лесной, ул. Фрунзе, 43'),
(N'410661, г. Лесной, ул. Школьная, 50'),
(N'625590, г. Лесной, ул. Коммунистическая, 20'),
(N'625683, г. Лесной, ул. 8 Марта'),
(N'450983, г.Лесной, ул. Комсомольская, 26'),
(N'394782, г. Лесной, ул. Чехова, 3'),
(N'603002, г. Лесной, ул. Дзержинского, 28'),
(N'450558, г. Лесной, ул. Набережная, 30'),
(N'344288, г. Лесной, ул. Чехова, 1'),
(N'614164, г.Лесной,  ул. Степная, 30'),
(N'394242, г. Лесной, ул. Коммунистическая, 43'),
(N'660540, г. Лесной, ул. Солнечная, 25'),
(N'125837, г. Лесной, ул. Шоссейная, 40'),
(N'125703, г. Лесной, ул. Партизанская, 49'),
(N'625283, г. Лесной, ул. Победы, 46'),
(N'614753, г. Лесной, ул. Полевая, 35'),
(N'426030, г. Лесной, ул. Маяковского, 44'),
(N'450375, г. Лесной ул. Клубная, 44'),
(N'625560, г. Лесной, ул. Некрасова, 12'),
(N'630201, г. Лесной, ул. Комсомольская, 17'),
(N'190949, г. Лесной, ул. Мичурина, 26');
GO
INSERT INTO dbo.Clients (FullName) VALUES
(N'Ворсин Петр Евгеньевич'),
(N'Сазонов Руслан Германович'),
(N'Старикова Елена Павловна'),
(N'Степанов Михаил Артёмович');
GO

INSERT INTO dbo.Products
(Article, Title, UnitID, Cost, SupplierID, ManufacturerID, CategoryID, Discount, Quantity, Description, PhotoPath)
VALUES
(N'PMEZMH', N'Детский игровой набор машинок Щенячий патруль / Dogs mini . 9 героев + 9 инерфионных машинок', 1, 1414, 3, 1, 2, 22, 50, N'Детский набор машинок с героями мультсериала «Щенячий патруль» подойдет как для мальчиков, так и для девочек. В детский набор входит 9 фигурок щенков спасателей.', N'Images/1.jpg'),
(N'BPV4MM', N'Конструктор Гарри Поттер Сова Букля 630 деталей совместим с lego harry potter, лего совместимый)', 1, 771, 4, 1, 3, 15, 26, N'Коллекционная модель Букля состоит из множества потрясающих элементов, а также специального механизма внутри. С его помощью можно плавно поднимать-опускать крылья птицы.', N'Images/2.jpg'),
(N'JVL42J', N'Музыкальные инструменты для детей, ксилофон, барабаны, развивающие игрушки, игрушки для детей', 1, 2750, 4, 2, 1, 15, 0, N'Откройте мир музыки для вашего ребенка с этой уникальной игрушкой! Это многофункциональное музыкальное чудо объединяет в себе всё, что нужно для творческого развития.', N'Images/3.jpg'),
(N'F895RB', N'Машинка игрушка диско шар светящаяся музыкальная', 1, 368, 2, 1, 4, 6, 7, N'Светящаяся музыкальная машина с диско шаром переливается разными цветами, играет ритмичные мелодии, объезжает препятствия и крутится, поэтому с ней точно не будет скучно.', N'Images/4.jpg'),
(N'3XBOTN', N'Игровой набор Hot Wheels Action Loop Cyclone Challenge Track, с машинкой и удобным хранением, HTK16', 1, 3426, 2, 2, 2, 10, 21, N'Игровой набор Hot Wheels Action Loop Cyclone Challenge Track - это уникальная игра, которая позволит вам испытать себя и своих друзей в скорости и ловкости. Этот набор состоит из металлической дорожки с циклоном, которая создает потрясающий эффект и добавляет дополнительную сложность в игру.', N'Images/5.jpg'),
(N'3L7RCZ', N'Игровой набор с деревянными машинками Стройплощадка Кран-Паркс, Junion', 1, 7400, 2, 3, 2, 15, 0, N'Игровой набор «Стройплощадка Кран-Паркс Junion» — это большая игрушечная парковка с деревянными машинками и настоящим подъёмным краном, придуманная в Яндексе настоящими родителями.', N'Images/6.jpg'),
(N'S72AM3', N'Синтезатор детский с микрофоном 61 клавиша', 1, 1749, 1, 3, 1, 10, 35, N'Откройте для ребенка дверь в мир музыки с детским синтезатором! Этот компактный инструмент с микрофоном станет верным другом для юных музыкантов, помогая им развивать творческий потенциал и получать удовольствие от игры.', N'Images/7.jpg'),
(N'2G3280', N'Деревянный игровой набор JUNION Стройплощадка "Кран-Паркс" с подъёмным, строительным краном и машинками, 18 предметов, подвижные элементы', 1, 1624, 5, 3, 2, 9, 20, N'Игровой набор «Стройплощадка Кран-Паркс Junion» — это большая игрушечная парковка с деревянными машинками и настоящим подъёмным краном, придуманная в Яндексе настоящими родителями.', N'Images/8.jpg'),
(N'MIO8YV', N'Музыкальная игрушка интерактивная Пульт, детский прорезыватель для малышей', 1, 305, 5, 2, 1, 9, 31, N'Музыкальная игрушка интерактивная Пульт, детский прорезыватель для малышей', N'Images/9.jpg'),
(N'UER2QD', N'Большой набор опытов и экспериментов для детей 14 в 1', 1, 2506, 5, 2, 2, 8, 27, N'Большой набор опытов и экспериментов для детей 14 в 1', N'Images/10.jpg');
GO

SET IDENTITY_INSERT dbo.Orders ON;
INSERT INTO dbo.Orders (ID, OrderDate, DeliveryDate, PickupPointID, ClientID, ReceiveCode, StatusID)
VALUES
(1, '2025-02-27', '2025-04-20', 1, 4, 901, 1),
(2, '2024-09-28', '2025-04-21', 11, 1, 902, 1),
(3, '2025-03-21', '2025-04-22', 2, 3, 903, 1),
(4, '2025-02-20', '2025-04-23', 11, 2, 904, 1),
(5, '2025-03-17', '2025-04-24', 2, 4, 905, 1),
(6, '2025-03-01', '2025-04-25', 15, 1, 906, 1),
(7, N'30.02.2025', '2025-04-26', 3, 3, 907, 1),
(8, '2025-03-31', '2025-04-27', 19, 2, 908, 2),
(9, '2025-04-02', '2025-04-28', 5, 3, 909, 2),
(10, '2025-04-03', '2025-04-29', 19, 2, 910, 2);
SET IDENTITY_INSERT dbo.Orders OFF;
GO

INSERT INTO dbo.OrderItems (OrderID, ProductID, Quantity) VALUES
(1, 1, 2),
(1, 2, 2),
(2, 3, 1),
(2, 4, 1),
(3, 5, 10),
(3, 6, 10),
(4, 7, 5),
(4, 8, 4),
(5, 9, 2),
(5, 10, 2),
(6, 1, 2),
(6, 2, 2),
(7, 3, 1),
(7, 4, 1),
(8, 5, 10),
(8, 6, 10),
(9, 7, 5),
(9, 8, 4),
(10, 9, 2),
(10, 10, 2);
GO

CREATE VIEW dbo.vLogin AS
SELECT u.ID, r.Title AS RoleTitle, u.FullName, u.Login, u.[Password]
FROM dbo.Users u
INNER JOIN dbo.Roles r ON r.ID = u.RoleID
WHERE u.IsActive = 1;
GO

CREATE VIEW dbo.vProductList AS
SELECT
    p.ID,
    p.Article,
    p.Title,
    c.Title AS CategoryTitle,
    p.Description,
    m.Title AS ManufacturerTitle,
    s.Title AS SupplierTitle,
    p.Cost,
    p.Discount,
    CAST(p.Cost * (1 - p.Discount / 100.0) AS DECIMAL(10,2)) AS FinalCost,
    u.Title AS UnitTitle,
    p.Quantity,
    p.PhotoPath,
    p.IsActive,
    p.CategoryID,
    p.ManufacturerID,
    p.SupplierID,
    p.UnitID
FROM dbo.Products p
INNER JOIN dbo.Categories c ON c.ID = p.CategoryID
INNER JOIN dbo.Manufacturers m ON m.ID = p.ManufacturerID
INNER JOIN dbo.Suppliers s ON s.ID = p.SupplierID
INNER JOIN dbo.Units u ON u.ID = p.UnitID
WHERE p.IsActive = 1;
GO

CREATE VIEW dbo.vOrderList AS
SELECT
    o.ID,
    STRING_AGG(CONCAT(p.Article, N', ', oi.Quantity), N', ') AS Articles,
    o.OrderDate,
    o.DeliveryDate,
    pp.ID AS PickupPointID,
    pp.Address AS PickupPointAddress,
    cl.FullName AS ClientFullName,
    o.ReceiveCode,
    os.ID AS StatusID,
    os.Title AS StatusTitle,
    SUM(oi.Quantity) AS TotalQuantity,
    SUM(oi.Quantity * p.Cost * (1 - p.Discount / 100.0)) AS TotalCost
FROM dbo.Orders o
INNER JOIN dbo.PickupPoints pp ON pp.ID = o.PickupPointID
LEFT JOIN dbo.Clients cl ON cl.ID = o.ClientID
INNER JOIN dbo.OrderStatuses os ON os.ID = o.StatusID
LEFT JOIN dbo.OrderItems oi ON oi.OrderID = o.ID
LEFT JOIN dbo.Products p ON p.ID = oi.ProductID
GROUP BY o.ID, o.OrderDate, o.DeliveryDate, pp.ID, pp.Address, cl.FullName, o.ReceiveCode, os.ID, os.Title;
GO
