CREATE TABLE IF NOT EXISTS SavingAccount(
    UserID INTEGER NOT NULL,
    AccID INTEGER PRIMARY KEY UNIQUE,
    Balance INTEGER NOT NULL DEFAULT 0,
    Student BOOLEAN NOT NULL DEFAULT FALSE,
    FOREIGN KEY (UserID) REFERENCES Users(UserID)
);