CREATE SCHEMA mail;

ALTER
    SCHEMA mail OWNER to test;

CREATE SEQUENCE mail.directory_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE CACHE 1;

ALTER SEQUENCE mail.directory_id_seq OWNER to test;

CREATE TABLE mail."Directory"
(
    "Id"       int DEFAULT nextval('mail.directory_id_seq'::regclass) NOT NULL,
    "Name"     character varying(64)                                  NOT NULL,
    "ParentId" int
);

ALTER TABLE mail."Directory"
    OWNER to test;

CREATE INDEX "Directory_Id_Idx" ON mail."Directory" ("Id");
CREATE INDEX "Directory_ParentId_Idx" ON mail."Directory" ("ParentId");
CREATE UNIQUE INDEX "Directory_ParentId_UniqueIdx" ON mail."Directory" ("ParentId", "Name");

ALTER TABLE ONLY mail."Directory"
    ADD CONSTRAINT "Directory_pkey" PRIMARY KEY ("Id");

ALTER TABLE ONLY mail."Directory"
    ADD CONSTRAINT "Directory_ParentId_fk "
        FOREIGN KEY ("ParentId")
            REFERENCES mail."Directory" ("Id") ON DELETE CASCADE;

CREATE SEQUENCE mail.message_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE CACHE 1;

ALTER SEQUENCE mail.message_id_seq OWNER to test;

CREATE TABLE mail."Message"
(
    "Id"          int DEFAULT nextval('mail.message_id_seq'::regclass) NOT NULL,
    "DirectoryId" int                                                  NOT NULL,
    "Sender"      character varying(64)                                NOT NULL,
    "To"          character varying[]                                  NOT NULL,
    "Cc"          character varying[],
    "Bcc"         character varying[],
    "Subject"     character varying(64)                                NOT NULL,
    "TextBody"    text,
    "HtmlBody"    text
);

ALTER TABLE mail."Message"
    OWNER to test;

CREATE INDEX "Message_Id_Idx" ON mail."Message" ("Id");
CREATE INDEX "Message_DirectoryId_Idx" ON mail."Message" ("Id");

ALTER TABLE ONLY mail."Message"
    ADD CONSTRAINT "Message_pk" PRIMARY KEY ("Id");

ALTER TABLE ONLY mail."Message"
    ADD CONSTRAINT "Message_DirectoryId_fk"
        FOREIGN KEY ("DirectoryId")
            REFERENCES mail."Directory" ("Id") ON DELETE CASCADE;

CREATE SEQUENCE mail.attachment_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE CACHE 1;

ALTER SEQUENCE mail.attachment_id_seq OWNER to test;

CREATE TABLE mail."Attachment"
(
    "Id"        int DEFAULT nextval('mail.attachment_id_seq'::regclass) NOT NULL,
    "MessageId" int                                                     NOT NULL,
    "Filename"  character varying(64)                                   NOT NULL,
    "Oid"       oid                                                     NOT NULL
);

ALTER TABLE mail."Attachment"
    OWNER to test;

CREATE INDEX "Attachment_Id_Idx" ON mail."Attachment" ("Id");
CREATE INDEX "Attachment_MessageId_Idx" ON mail."Attachment" ("MessageId");

ALTER TABLE ONLY mail."Attachment"
    ADD CONSTRAINT "Attachment_pkey" PRIMARY KEY ("Id");

ALTER TABLE ONLY mail."Attachment"
    ADD CONSTRAINT "Attachment_MessageId_fk"
        FOREIGN KEY ("MessageId")
            REFERENCES mail."Message" ("Id") ON DELETE CASCADE;

CREATE OR REPLACE VIEW mail."Messages"
AS
SELECT "Message"."Id",
       "Message"."DirectoryId",
       "Directory"."Name"      AS "Directory",
       "Directory"."ParentId"  AS "ParentId",
       "Message"."Sender",
       unnest("Message"."To")  AS "To",
       unnest("Message"."Cc")  AS "Cc",
       unnest("Message"."Bcc") AS "Bcc",
       "Message"."Subject",
       "Message"."TextBody",
       "Message"."HtmlBody",
       "Attachment"."Id"       AS "AttachmentId",
       "Attachment"."Filename" AS "Attachment"
FROM mail."Message"
         INNER JOIN mail."Directory" ON "Directory"."Id" = "Message"."DirectoryId"
         LEFT JOIN mail."Attachment" ON "Attachment"."MessageId" = "Message"."Id";

ALTER TABLE mail."Messages"
    OWNER TO test;