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