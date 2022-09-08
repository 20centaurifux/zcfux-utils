CREATE
    EXTENSION IF NOT EXISTS lo;

CREATE SCHEMA mail;

ALTER
    SCHEMA mail OWNER to test;

CREATE SEQUENCE mail.message_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE CACHE 1;

ALTER SEQUENCE mail.message_id_seq OWNER to test;

CREATE TABLE mail."Message"
(
    "Id"       int DEFAULT nextval('mail.message_id_seq'::regclass) NOT NULL,
    "Sender"   character varying(50)                                NOT NULL,
    "To"       character varying[]                                  NOT NULL,
    "Cc"       character varying[],
    "Bcc"      character varying[],
    "Subject"  character varying(100)                               NOT NULL,
    "TextBody" text,
    "HtmlBody" text
);

ALTER TABLE mail."Message"
    OWNER to test;

ALTER TABLE ONLY mail."Message"
    ADD CONSTRAINT "Message_pk" PRIMARY KEY ("Id");

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
    "Filename"  character varying(50)                                   NOT NULL,
    "Oid"       lo                                                      NOT NULL
);

ALTER TABLE mail."Attachment"
    OWNER to test;

CREATE INDEX "Attachment_MessageId_Idx" ON mail."Attachment" ("MessageId");

ALTER TABLE ONLY mail."Attachment"
    ADD CONSTRAINT "Attachment_pkey" PRIMARY KEY ("Id");

ALTER TABLE ONLY mail."Attachment"
    ADD CONSTRAINT "Attachment_MessageId_fk"
        FOREIGN KEY ("MessageId")
            REFERENCES mail."Message" ("Id") ON
            DELETE
            CASCADE;

CREATE TRIGGER t_attachment_oid
    BEFORE UPDATE OR
        DELETE
    ON mail."Attachment"
    FOR EACH ROW
EXECUTE FUNCTION lo_manage("Oid");

CREATE SEQUENCE mail.directory_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE CACHE 1;

ALTER SEQUENCE mail.directory_id_seq OWNER to test;

CREATE TABLE mail."Directory"
(
    "Id"       int DEFAULT nextval('mail.directory_id_seq'::regclass) NOT NULL,
    "Name"     character varying(50)                                  NOT NULL,
    "ParentId" int
);

ALTER TABLE mail."Directory"
    OWNER to test;

CREATE INDEX "Directory_ParentId_Idx" ON mail."Directory" ("ParentId");
CREATE UNIQUE INDEX "Directory_ParentId_UniqueIdx" ON mail."Directory" ("ParentId", "Name");

ALTER TABLE ONLY mail."Directory"
    ADD CONSTRAINT "Directory_pkey" PRIMARY KEY ("Id");

ALTER TABLE ONLY mail."Directory"
    ADD CONSTRAINT "Directory_ParentId_fk "
        FOREIGN KEY ("ParentId")
            REFERENCES mail."Directory" ("Id") ON
            DELETE
            CASCADE;

CREATE TABLE mail."DirectoryEntry"
(
    "DirectoryId" int NOT NULL,
    "MessageId"   int NOT NULL
);

ALTER TABLE mail."DirectoryEntry"
    OWNER to test;

ALTER TABLE ONLY mail."DirectoryEntry"
    ADD CONSTRAINT "DirectoryEntry_pkey" PRIMARY KEY ("DirectoryId", "MessageId");

ALTER TABLE ONLY mail."DirectoryEntry"
    ADD CONSTRAINT "DirectoryEntry_DirectoryId_fk "
        FOREIGN KEY ("DirectoryId")
            REFERENCES mail."Directory" ("Id") ON
            DELETE
            CASCADE;

ALTER TABLE ONLY mail."DirectoryEntry"
    ADD CONSTRAINT "DirectoryEntry_MessageId_fk "
        FOREIGN KEY ("MessageId")
            REFERENCES mail."Message" ("Id") ON
            DELETE
            CASCADE;

CREATE SEQUENCE mail.queue_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE CACHE 1;

ALTER SEQUENCE mail.queue_id_seq OWNER to test;

CREATE TABLE mail."Queue"
(
    "Id"   int DEFAULT nextval('mail.queue_id_seq'::regclass) NOT NULL,
    "Name" character varying(50)                              NOT NULL
);

ALTER TABLE mail."Queue"
    OWNER to test;

ALTER TABLE ONLY mail."Queue"
    ADD CONSTRAINT "Queue_pkey" PRIMARY KEY ("Id");

CREATE TABLE mail."QueueItem"
(
    "QueueId"   int           NOT NULL,
    "MessageId" int           NOT NULL,
    "Created"   timestamp     NOT NULL,
    "EndOfLife" timestamp     NOT NULL,
    "NextDue"   timestamp,
    "Errors"    int DEFAULT 0 NOT NULL
);

ALTER TABLE mail."QueueItem"
    OWNER to test;

CREATE INDEX "QueueItem_NextDue_Idx" ON mail."QueueItem" ("NextDue");

ALTER TABLE ONLY mail."QueueItem"
    ADD CONSTRAINT "QueueItem_pkey" PRIMARY KEY ("QueueId", "MessageId");

ALTER TABLE ONLY mail."QueueItem"
    ADD CONSTRAINT "QueueItem_QueueId_fk "
        FOREIGN KEY ("QueueId")
            REFERENCES mail."Queue" ("Id") ON
            DELETE
            CASCADE;

ALTER TABLE ONLY mail."QueueItem"
    ADD CONSTRAINT "QueueItem_MessageId_fk "
        FOREIGN KEY ("MessageId")
            REFERENCES mail."Message" ("Id") ON
            DELETE
            CASCADE;

CREATE OR REPLACE VIEW mail."FlatStoredMessages"
AS
SELECT "Message"."Id",
       "Directory"."Id"        AS "DirectoryId",
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
FROM mail."DirectoryEntry"
         INNER JOIN mail."Directory" ON "DirectoryEntry"."DirectoryId" = "Directory"."Id"
         INNER JOIN mail."Message" ON "DirectoryEntry"."MessageId" = "Message"."Id"
         LEFT JOIN mail."Attachment" ON "Attachment"."MessageId" = "Message"."Id";

ALTER
    VIEW mail."FlatStoredMessages"
    OWNER TO test;

CREATE OR REPLACE VIEW mail."StoredMessages"
AS
SELECT "Message"."Id",
       "Directory"."Id"       AS "DirectoryId",
       "Directory"."Name"     AS "Directory",
       "Directory"."ParentId" AS "ParentId",
       "Message"."Sender",
       "Message"."To",
       "Message"."Cc"         AS "Cc",
       "Message"."Bcc"        AS "Bcc",
       "Message"."Subject",
       "Message"."TextBody",
       "Message"."HtmlBody"
FROM mail."DirectoryEntry"
         INNER JOIN mail."Directory" ON "DirectoryEntry"."DirectoryId" = "Directory"."Id"
         INNER JOIN mail."Message" ON "DirectoryEntry"."MessageId" = "Message"."Id"
         LEFT JOIN mail."Attachment" ON "Attachment"."MessageId" = "Message"."Id";

ALTER
    VIEW mail."StoredMessages"
    OWNER TO test;

CREATE OR REPLACE VIEW mail."FlatQueuedMessages"
AS
SELECT "Message"."Id",
       "QueueItem"."QueueId",
       "Queue"."Name"          AS "Queue",
       "Message"."Sender",
       unnest("Message"."To")  AS "To",
       unnest("Message"."Cc")  AS "Cc",
       unnest("Message"."Bcc") AS "Bcc",
       "Message"."Subject",
       "Message"."TextBody",
       "Message"."HtmlBody",
       "Attachment"."Id"       AS "AttachmentId",
       "Attachment"."Filename" AS "Attachment",
       "QueueItem"."Created",
       "QueueItem"."EndOfLife",
       "QueueItem"."NextDue",
       "QueueItem"."Errors"
FROM mail."QueueItem"
         INNER JOIN mail."Queue" ON "QueueItem"."QueueId" = "Queue"."Id"
         INNER JOIN mail."Message" ON "QueueItem"."MessageId" = "Message"."Id"
         LEFT JOIN mail."Attachment" ON "Attachment"."MessageId" = "Message"."Id";

ALTER
    VIEW mail."FlatQueuedMessages"
    OWNER TO test;

CREATE OR REPLACE VIEW mail."QueuedMessages"
AS
SELECT "Message"."Id",
       "QueueItem"."QueueId",
       "Queue"."Name"          AS "Queue",
       "Message"."Sender",
       "Message"."To"          AS "To",
       "Message"."Cc"          AS "Cc",
       "Message"."Bcc"         AS "Bcc",
       "Message"."Subject",
       "Message"."TextBody",
       "Message"."HtmlBody",
       "Attachment"."Id"       AS "AttachmentId",
       "Attachment"."Filename" AS "Attachment",
       "QueueItem"."Created",
       "QueueItem"."EndOfLife",
       "QueueItem"."NextDue",
       "QueueItem"."Errors"
FROM mail."QueueItem"
         INNER JOIN mail."Queue" ON "QueueItem"."QueueId" = "Queue"."Id"
         INNER JOIN mail."Message" ON "QueueItem"."MessageId" = "Message"."Id"
         LEFT JOIN mail."Attachment" ON "Attachment"."MessageId" = "Message"."Id";

ALTER
    VIEW mail."QueuedMessages"
    OWNER TO test;