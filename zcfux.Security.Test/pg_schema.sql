CREATE SCHEMA "security";

ALTER
    SCHEMA "security" OWNER to test;

CREATE TABLE "security"."TokenKind"
(
    "Id" int NOT NULL,
    "Name" character varying(30) NOT NULL
);

ALTER TABLE "security"."TokenKind"
    OWNER TO test;

ALTER TABLE ONLY "security"."TokenKind"
    ADD CONSTRAINT "TokenKind_pkey" PRIMARY KEY ("Id");

CREATE UNIQUE INDEX "TokenKind_Name_Uidx" ON "security"."TokenKind" ("Name");

CREATE TABLE "security"."Token"
(
    "KindId" int NOT NULL,
    "Value" character varying(50) NOT NULL,
    "Counter" int NOT NULL DEFAULT 0,
    "EndOfLife" timestamp
);

ALTER TABLE "security"."Token"
    OWNER TO test;

ALTER TABLE ONLY "security"."Token"
    ADD CONSTRAINT "Token_pkey" PRIMARY KEY ("KindId", "Value");

ALTER TABLE ONLY "security"."Token"
    ADD CONSTRAINT "Token_KindId_fk"
        FOREIGN KEY ("KindId")
            REFERENCES "security"."TokenKind" ("Id") ON
            DELETE
            CASCADE;

CREATE OR REPLACE VIEW "security"."Tokens"
AS
SELECT t."KindId", k."Name" "Kind", t."Value", t."Counter", t."EndOfLife"
	FROM "security"."Token" t
	INNER JOIN "security"."TokenKind" k ON t."KindId" = k."Id";

    
ALTER VIEW "security"."Tokens"
    OWNER TO test;