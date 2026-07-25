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

CREATE TABLE IF NOT EXISTS orders (
    id             SERIAL PRIMARY KEY,
    chat_id        BIGINT NOT NULL,
    customer_name  VARCHAR(150) NOT NULL,
    customer_phone VARCHAR(30) NOT NULL,
    status         VARCHAR(20) NOT NULL DEFAULT 'new',
    created_at     TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS order_items (
    id           SERIAL PRIMARY KEY,
    order_id     INTEGER NOT NULL REFERENCES orders(id),
    product_id   INTEGER NOT NULL REFERENCES products(id),
    product_name VARCHAR(150) NOT NULL,
    unit_price   NUMERIC(10,2) NOT NULL,
    quantity     INTEGER NOT NULL
);

-- Тестовые данные
INSERT INTO categories (name) VALUES ('Платья'), ('Костюмы'), ('Куртки');
INSERT INTO products (name, category_id, price, size, color, stock_quantity)
VALUES
  ('Платье Снежинка', 1, 120.00, '92-98', 'белый', 12),
  ('Костюм Спорт', 2, 180.00, '104-110', 'синий', 10),
  ('Куртка Пуховик', 3, 320.00, '116-122', 'красный', 4);