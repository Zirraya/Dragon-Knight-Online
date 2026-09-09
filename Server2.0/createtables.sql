CREATE TABLE accounts
(
    id INT AUTO_INCREMENT PRIMARY KEY,

    username VARCHAR(64) NOT NULL UNIQUE,
    password VARCHAR(128) NOT NULL,

    created TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);


CREATE TABLE characters
(
    id INT AUTO_INCREMENT PRIMARY KEY,

    account_id INT NOT NULL,

    name VARCHAR(32) NOT NULL,

    class_id INT DEFAULT 1,
    gender INT DEFAULT 0,

    hair INT DEFAULT 0,
    face INT DEFAULT 0,
    hair_color INT DEFAULT 0,


    level INT DEFAULT 1,
    exp INT DEFAULT 0,


    hp INT DEFAULT 100,
    mp INT DEFAULT 100,


    x FLOAT DEFAULT 100,
    y FLOAT DEFAULT 200,
    z FLOAT DEFAULT 0,


    map INT DEFAULT 1,


    FOREIGN KEY(account_id)
    REFERENCES accounts(id)
    ON DELETE CASCADE
);
