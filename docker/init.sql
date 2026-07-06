-- docker/init.sql
CREATE TABLE IF NOT EXISTS categories (
    id   SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL
);

CREATE TABLE IF NOT EXISTS products (
    id             SERIAL PRIMARY KEY,
    name           VARCHAR(150) NOT NULL,
    category_id    INTEGER NOT NULL REFERENCES categories(id),
    price          NUMERIC(10,2) NOT NULL,
    size           VARCHAR(10),
    color          VARCHAR(30),
    stock_quantity INTEGER NOT NULL DEFAULT 0,
    created_at     TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Тестовые данные
INSERT INTO categories (name) VALUES ('Платья'), ('Костюмы'), ('Куртки');
INSERT INTO products (name, category_id, price, size, color, stock_quantity)
VALUES
  ('Платье Снежинка', 1, 120.00, '92-98', 'белый', 12),
  ('Костюм Спорт', 2, 180.00, '104-110', 'синий', 10),
  ('Куртка Пуховик', 3, 320.00, '116-122', 'красный', 4);