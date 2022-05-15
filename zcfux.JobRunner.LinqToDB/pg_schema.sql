CREATE SCHEMA scheduler;

ALTER SCHEMA scheduler OWNER to test;

CREATE SEQUENCE scheduler.job_kind_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE CACHE 1;

ALTER SEQUENCE scheduler.job_kind_id_seq OWNER to test;

CREATE TABLE scheduler."JobKind"
(
    "Id"       int DEFAULT nextval('scheduler.job_kind_id_seq'::regclass) NOT NULL,
    "Assembly" character varying(256)                                     NOT NULL,
    "FullName" character varying(256)                                     NOT NULL
);

ALTER TABLE scheduler."JobKind"
    OWNER to test;

ALTER TABLE ONLY scheduler."JobKind"
    ADD CONSTRAINT "JobKind_pkey" PRIMARY KEY ("Id");

CREATE UNIQUE INDEX "JobKind_AssemblyFullname_unique"
    ON scheduler."JobKind" ("Assembly", "FullName");

CREATE TABLE scheduler."Job"
(
    "Guid"     uuid                 NOT NULL,
    "KindId"   int                  NOT NULL,
    "Status"   smallint             NOT NULL,
    "Running"  boolean  DEFAULT 'F' NOT NULL,
    "Created"  timestamp            NOT NULL,
    "Args"     text[],
    "LastDone" timestamp,
    "NextDue"  timestamp,
    "Errors"   smallint DEFAULT 0   NOT NULL
);

ALTER TABLE scheduler."Job"
    OWNER to test;

ALTER TABLE ONLY scheduler."Job"
    ADD CONSTRAINT "Job_pkey" PRIMARY KEY ("Guid");

ALTER TABLE ONLY scheduler."Job"
    ADD CONSTRAINT "Job_KindId_fk"
        FOREIGN KEY ("KindId")
            REFERENCES scheduler."JobKind" ("Id");

CREATE INDEX "Job_KindId_Idx" ON scheduler."Job" ("KindId");

CREATE INDEX "Job_Running_Idx" ON scheduler."Job" ("Running");

CREATE INDEX "Job_Status_Idx" ON scheduler."Job" ("Status");

CREATE INDEX "Job_NextDue_Idx" ON scheduler."Job" ("NextDue");