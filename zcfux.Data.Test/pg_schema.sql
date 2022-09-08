CREATE SCHEMA test;

ALTER
    SCHEMA test OWNER to test;

CREATE SEQUENCE test.record_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE CACHE 1;

ALTER TABLE test.record_id_seq
    OWNER TO test;

CREATE TABLE test."Model"
(
    "ID"    integer DEFAULT nextval('test.record_id_seq'::regclass) NOT NULL,
    "Value" character varying(100)                                  NOT NULL
);

ALTER TABLE test."Model"
    OWNER TO test;

ALTER TABLE ONLY test."Model"
    ADD CONSTRAINT "Model_pkey" PRIMARY KEY ("ID");