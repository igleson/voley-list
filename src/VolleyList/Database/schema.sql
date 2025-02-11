
CREATE TABLE listing
(
    id                      TEXT,
    name                    TEXT,
    max_size                INT,
    limit_date_to_remove_name_And_not_pay TEXT,
    PRIMARY KEY (id)
);


CREATE TABLE listing_events
(
    name             VARCHAR(38),
    listing_id       VARCHAR(38),
    type             INT,
    participant_type INT,
    date             TEXT,
    PRIMARY KEY (name, listing_id, type, date),
    FOREIGN KEY (listing_id) REFERENCES listing (id)
)
