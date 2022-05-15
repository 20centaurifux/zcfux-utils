--
-- PostgreSQL database dump
--

-- Dumped from database version 13.4
-- Dumped by pg_dump version 13.4

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: record_id_seq; Type: SEQUENCE; Schema: public; Owner: test
--

CREATE SEQUENCE public.record_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.record_id_seq OWNER TO test;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: Model; Type: TABLE; Schema: public; Owner: test
--

CREATE TABLE public."Model" (
    "ID" integer DEFAULT nextval('public.record_id_seq'::regclass) NOT NULL,
    "Value" character varying(100) NOT NULL
);


ALTER TABLE public."Model" OWNER TO test;

--
-- Name: Model Model_pkey; Type: CONSTRAINT; Schema: public; Owner: test
--

ALTER TABLE ONLY public."Model"
    ADD CONSTRAINT "Model_pkey" PRIMARY KEY ("ID");


--
-- PostgreSQL database dump complete
--

