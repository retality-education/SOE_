-- Таблица чатов
CREATE TABLE chats (
    id VARCHAR(50) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    created_by VARCHAR(50) NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Участники чатов
CREATE TABLE chat_users (
    chat_id VARCHAR(50) REFERENCES chats(id) ON DELETE CASCADE,
    user_id VARCHAR(50) NOT NULL,
    joined_at TIMESTAMPTZ DEFAULT NOW(),
    PRIMARY KEY (chat_id, user_id)
);

-- Сообщения (обновлённая версия)
CREATE TABLE messages (
    id UUID PRIMARY KEY,
    chat_id VARCHAR(50) NOT NULL REFERENCES chats(id),
    user_id VARCHAR(50) NOT NULL,
    text TEXT NOT NULL,
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW()
);