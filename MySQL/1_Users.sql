CREATE TABLE IF NOT EXISTS Users (
    UserID INTEGER PRIMARY KEY AUTOINCREMENT,
    FirstName VARCHAR(255) NOT NULL,
    LastName VARCHAR(255) NOT NULL,
    Email VARCHAR(255) NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    Role ENUM('User', 'Banker', 'Admin') NOT NULL DEFAULT 'User',
    Bankrupt BOOLEAN NOT NULL DEFAULT 0
    -- If user doesnt pay his debt and his fonds from other users are not enough to cover it, he will be marked as 'Bankrupt' and wont be able to make any transactions
);
