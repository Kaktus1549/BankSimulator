CREATE TABLE IF NOT EXISTS FreeAccount(
    UserID INTEGER NOT NULL,
    AccID INTEGER PRIMARY KEY UNIQUE,
    Balance INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (UserID) REFERENCES Users(UserID)
);

-- /001 -> bezny ucet
-- /002 -> sporici ucet
-- /003 -> kreditni ucet