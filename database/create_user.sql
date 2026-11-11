-- Пользователь MySQL для приложения и тестов.
-- Права даются на базу phone_repair и временные базы тестов phone_repair_test_*.
CREATE USER IF NOT EXISTS 'phone_repair'@'localhost' IDENTIFIED BY 'phone_repair_pass';
GRANT ALL PRIVILEGES ON `phone\_repair%`.* TO 'phone_repair'@'localhost';
FLUSH PRIVILEGES;
