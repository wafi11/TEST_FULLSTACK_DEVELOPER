-- postgresql database version 14

create table customers (
    customer_id serial PRIMARY key,
    name VARCHAR(100) not NULL,
    email VARCHAR(100) UNIQUE not NULL,
    address TEXT,
    created_at TIMESTAMP DEFAULT current_timestamp
);

INSERT into customers (name,email,address) VALUES
('aan','aan@gmail.com','jakarta'),
('budi','budi@gmail.com','bekasi'),
('cathline','cathline@gmail.com','jepara');

create index idx_customers_name on customers(name);


CREATE TABLE stocks (
    stock_id SERIAL PRIMARY KEY,
    code char(6) unique not NULL,
    company_name VARCHAR(100)  not NULL,
    buy_price DECIMAL(15,2)  not NULL,
    sell_price DECIMAL(15,2) not NULL,
    updated_at TIMESTAMP DEFAULT current_timestamp
);

insert into stocks (code,company_name,buy_price,sell_price) VALUES
('BBBCAA','PT. Maju Jaya', 70000.00,75000.00),
('BBBCAB','PT. Maju Sejahtera', 900.00,1000.00),
('BBBCAC','PT. Maju Alami', 2400.00,2510.20);



create index idx_stocks_company_name on stocks(company_name);

create table transactions (
    trx_id serial PRIMARY KEY,
    reference_id VARCHAR(100) unique not NULL,
    status VARCHAR(20) not NULL,
    customer_id int REFERENCES customers(customer_id),
    stock_id int REFERENCES stocks(stock_id),
    transaction_type VARCHAR(20)  not NULL,
    price_per_lot DECIMAL(15,2) not null,
    quantity_lot int not NULL,
    total DECIMAL(15,2) not NULL,
    created_at TIMESTAMP DEFAULT current_timestamp
);

create INDEX idx_transactions_status on transactions(status);
create INDEX idx_transactions_transaction_type on transactions(transaction_type);


INSERT into transactions (reference_id,status,customer_id,stock_id,
transaction_type,price_per_lot,quantity_lot,total) VALUES
('TRX001','success',1,1,'buy',70000.00,1,70000.00),
('TRX002','success',2,2,'buy',900.00,2,1800.00),
('TRX003','success',2,2,'sell',900.00,1,900.00),
('TRX004','success',3,3,'buy',2400.00,1,2400.00);


select * from customers;
select * from stocks;
select * from transactions;


-- query check all customers saham

SELECT 
  ROW_NUMBER() OVER (ORDER BY c.name) AS "NO. ",
  c.name as "Nama Customer",
  concat(
    'Rp ',
    to_char(
        COALESCE(
            sum(
                CASE 
                    WHEN t.transaction_type = 'buy' THEN t.quantity_lot  
                    WHEN t.transaction_type = 'sell' THEN -t.quantity_lot  
                    ELSE  0
                END
            ) * s.sell_price,
        0),
        'FM999,999,999,990.00'
    ),
    ', -'
  ) as "Total Kekayaan"
  FROM customers c 
  LEFT JOIN transactions t on c.customer_id = t.customer_id 
  AND t.status = 'success'
  LEFT JOIN stocks s on s.stock_id = t.stock_id
  GROUP BY c.customer_id,c.name,s.sell_price
  ORDER BY c.name;