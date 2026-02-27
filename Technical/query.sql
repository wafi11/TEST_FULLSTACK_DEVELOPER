CREATE TABLE customers (
      customer_id INT           PRIMARY KEY AUTO_INCREMENT,
      name        VARCHAR(100)  NOT NULL,
      email       VARCHAR(100)  NOT NULL,
      address     TEXT,
      created_at  TIMESTAMP     DEFAULT CURRENT_TIMESTAMP
  );

  CREATE TABLE stocks (
      stock_id     INT          PRIMARY KEY AUTO_INCREMENT,
      code         CHAR(6)      NOT NULL UNIQUE,
      company_name VARCHAR(100) NOT NULL,
      buy_price    DECIMAL(15,2) NOT NULL,
      sell_price   DECIMAL(15,2) NOT NULL,
      updated_at   TIMESTAMP    DEFAULT CURRENT_TIMESTAMP
                                ON UPDATE CURRENT_TIMESTAMP
  );

  CREATE TABLE transactions (
      trx_id            INT           PRIMARY KEY AUTO_INCREMENT,
      reference_id      VARCHAR(100)  NOT NULL,
      status            VARCHAR(20)   NOT NULL DEFAULT 'pending',
      customer_id       INT           NOT NULL,
      stock_id          INT           NOT NULL,
      price_buy_per_lot DECIMAL(15,2) NOT NULL,
      quantity_lot      INT           NOT NULL,
      transaction_type  VARCHAR(10)   NOT NULL,
      total             DECIMAL(15,2) NOT NULL,
      created_at        TIMESTAMP     DEFAULT CURRENT_TIMESTAMP,

      CONSTRAINT fk_transactions_customer
          FOREIGN KEY (customer_id) REFERENCES customers(customer_id)
          ON DELETE RESTRICT ON UPDATE CASCADE,

      CONSTRAINT fk_transactions_stock
          FOREIGN KEY (stock_id) REFERENCES stocks(stock_id)
          ON DELETE RESTRICT ON UPDATE CASCADE
  );


SELECT
      c.customer_id,
      c.name,
      COALESCE(SUM(
          CASE t.transaction_type
              WHEN 'buy'  THEN  t.total
              WHEN 'sell' THEN -t.total
              ELSE 0
          END
      ), 0) AS total_kekayaan
  FROM customers c
  LEFT JOIN transactions t
      ON c.customer_id = t.customer_id
      AND t.status = 'success'
  GROUP BY c.customer_id, c.name
  ORDER BY total_kekayaan DESC;