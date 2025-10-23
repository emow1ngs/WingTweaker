-- Создание базы данных для WingTweaker
CREATE DATABASE IF NOT EXISTS wingtweaker_keys CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE wingtweaker_keys;

-- Таблица ключей
CREATE TABLE IF NOT EXISTS keys (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    KeyValue VARCHAR(255) NOT NULL UNIQUE,
    MachineId VARCHAR(255) NOT NULL,
    CreatedDate DATETIME NOT NULL,
    ExpiryDate DATETIME NOT NULL,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    KeyType VARCHAR(100) NOT NULL,
    CustomerTelegram VARCHAR(255),
    Price DECIMAL(10,2) NOT NULL,
    INDEX idx_key_value (KeyValue),
    INDEX idx_machine_id (MachineId),
    INDEX idx_customer_telegram (CustomerTelegram),
    INDEX idx_expiry_date (ExpiryDate)
);

-- Таблица типов ключей
CREATE TABLE IF NOT EXISTS key_types (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    DurationDays INT NOT NULL,
    Price DECIMAL(10,2) NOT NULL,
    Description TEXT,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE
);

-- Таблица продаж
CREATE TABLE IF NOT EXISTS sales (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    KeyId INT NOT NULL,
    SaleDate DATETIME NOT NULL,
    Amount DECIMAL(10,2) NOT NULL,
    CustomerTelegram VARCHAR(255),
    Status VARCHAR(50) NOT NULL DEFAULT 'Completed',
    FOREIGN KEY (KeyId) REFERENCES keys(Id) ON DELETE CASCADE,
    INDEX idx_sale_date (SaleDate),
    INDEX idx_customer_telegram (CustomerTelegram)
);

-- Вставляем стандартные типы ключей
INSERT INTO key_types (Name, DurationDays, Price, Description, IsActive) VALUES
('Тест период (1 час)', 0, 0.00, 'Тестовый период на 1 час', TRUE),
('День', 1, 50.00, 'Доступ на 1 день', TRUE),
('Неделя', 7, 200.00, 'Доступ на 1 неделю', TRUE),
('Месяц', 30, 500.00, 'Доступ на 1 месяц', TRUE),
('Год', 365, 2000.00, 'Доступ на 1 год', TRUE)
ON DUPLICATE KEY UPDATE Name=Name;
