CREATE SCHEMA audit;

ALTER
    SCHEMA audit OWNER to test;

CREATE TABLE audit."TopicKind"
(
    "Id"   int                   NOT NULL,
    "Name" character varying(30) NOT NULL
);

ALTER TABLE audit."TopicKind"
    OWNER to test;

ALTER TABLE ONLY audit."TopicKind"
    ADD CONSTRAINT "TopicKind_pkey" PRIMARY KEY ("Id");

ALTER TABLE ONLY audit."TopicKind"
    ADD CONSTRAINT unique_topic_kind_name UNIQUE ("Name");

CREATE SEQUENCE audit.topic_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE CACHE 1;

ALTER SEQUENCE audit.topic_id_seq OWNER to test;

CREATE TABLE audit."Topic"
(
    "Id"          bigint DEFAULT nextval('audit.topic_id_seq'::regclass) NOT NULL,
    "KindId"      int                                                    NOT NULL,
    "DisplayName" character varying(50)                                  NOT NULL
);

ALTER TABLE audit."Topic"
    OWNER to test;

ALTER TABLE ONLY audit."Topic"
    ADD CONSTRAINT "Topic_pkey" PRIMARY KEY ("Id");

ALTER TABLE ONLY audit."Topic"
    ADD CONSTRAINT "Topic_KindId_fk"
        FOREIGN KEY ("KindId")
            REFERENCES audit."TopicKind" ("Id");

CREATE INDEX "Topic_KindId_Idx" ON audit."Topic" ("KindId");

CREATE VIEW audit."TopicView"
AS
SELECT t."Id",
       k."Id"   AS "KindId",
       k."Name" AS "Kind",
       t."DisplayName"
FROM audit."Topic" t
         JOIN audit."TopicKind" k ON k."Id" = t."KindId";

ALTER TABLE audit."TopicView"
    OWNER to test;

CREATE TABLE audit."Association"
(
    "Id"   integer               NOT NULL,
    "Name" character varying(30) NOT NULL
);

ALTER TABLE audit."Association"
    OWNER to test;

ALTER TABLE ONLY audit."Association"
    ADD CONSTRAINT "Association_pkey" PRIMARY KEY ("Id");

CREATE SEQUENCE audit.topic_association_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE CACHE 1;

ALTER SEQUENCE audit."topic_association_id_seq" OWNER to test;

CREATE TABLE audit."TopicAssociation"
(
    "Id"            bigint  DEFAULT nextval('audit.topic_association_id_seq'::regclass) NOT NULL,
    "Archived"      boolean DEFAULT ('F')                                               NOT NULL,
    "Topic1"        bigint                                                              NOT NULL,
    "AssociationId" integer                                                             NOT NULL,
    "Topic2"        bigint                                                              NOT NULL,
    PRIMARY KEY ("Id", "Archived")
) PARTITION BY LIST ("Archived");

ALTER TABLE audit."TopicAssociation"
    OWNER to test;

CREATE INDEX "TopicAssociation_Topic1_Idx" ON audit."TopicAssociation" ("Topic1");
CREATE INDEX "TopicAssociation_AssociationId_Idx" ON audit."TopicAssociation" ("AssociationId");
CREATE INDEX "TopicAssociation_Topic2_Idx" ON audit."TopicAssociation" ("Topic2");

ALTER TABLE audit."TopicAssociation"
    ADD CONSTRAINT "TopicAssociation_Topic1_fk"
        FOREIGN KEY ("Topic1")
            REFERENCES audit."Topic" ("Id");

ALTER TABLE audit."TopicAssociation"
    ADD CONSTRAINT "TopicAssociation_AssociationId_fk"
        FOREIGN KEY ("AssociationId")
            REFERENCES audit."Association" ("Id");

ALTER TABLE audit."TopicAssociation"
    ADD CONSTRAINT "TopicAssociation_Topic2_fk"
        FOREIGN KEY ("Topic2")
            REFERENCES audit."Topic" ("Id");

CREATE TABLE audit."RecentTopicAssociation" PARTITION OF audit."TopicAssociation" FOR VALUES IN
    (
    'F'
    );

ALTER TABLE audit."RecentTopicAssociation"
    OWNER to test;

CREATE TABLE audit."ArchivedTopicAssociation" PARTITION OF audit."TopicAssociation" FOR VALUES IN
    (
    'T'
    );

ALTER TABLE audit."ArchivedTopicAssociation"
    OWNER to test;

CREATE TABLE "audit"."EventKind"
(
    "Id"   int                   NOT NULL,
    "Name" character varying(30) NOT NULL,
    PRIMARY KEY ("Id")
);

ALTER TABLE audit."EventKind"
    OWNER to test;

CREATE SEQUENCE audit.event_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE CACHE 1;

ALTER SEQUENCE audit.event_id_seq OWNER to test;

CREATE TABLE audit."Event"
(
    "Id"        bigint    DEFAULT nextval('audit.event_id_seq'::regclass) NOT NULL,
    "KindId"    int                                                       NOT NULL,
    "Severity"  smallint                                                  NOT NULL,
    "Archived"  boolean   DEFAULT ('F')                                   NOT NULL,
    "CreatedAt" timestamp DEFAULT NOW()                                   NOT NULL,
    "TopicId"   bigint,
    PRIMARY KEY ("Id", "Archived")
) PARTITION BY LIST ("Archived");

ALTER TABLE audit."Event"
    OWNER to test;

CREATE INDEX "Event_KindId_Idx" ON audit."Event" ("KindId");
CREATE INDEX "Event_TopicId_Idx" ON audit."Event" ("TopicId");
CREATE INDEX "Event_Severity_Idx" ON audit."Event" ("Severity");
CREATE INDEX "Event_CreatedAt_Idx" ON audit."Event" ("CreatedAt" DESC);

ALTER TABLE audit."Event"
    ADD CONSTRAINT "Event_KindId_fk"
        FOREIGN KEY ("KindId")
            REFERENCES audit."EventKind" ("Id");

ALTER TABLE audit."Event"
    ADD CONSTRAINT "Event_TopicId_fk"
        FOREIGN KEY ("TopicId")
            REFERENCES audit."Topic" ("Id");

CREATE TABLE audit."RecentEvent" PARTITION OF audit."Event" FOR VALUES IN
    (
    'F'
    );

ALTER TABLE audit."RecentEvent"
    OWNER to test;

CREATE TABLE audit."ArchivedEvent" PARTITION OF audit."Event" FOR VALUES IN
    (
    'T'
    );

ALTER TABLE audit."ArchivedEvent"
    OWNER to test;

CREATE OR REPLACE VIEW audit."RecentEventView"
AS
SELECT e."Id",
       e."KindId",
       k."Name" AS "Kind",
       e."CreatedAt",
       e."Severity",
       e."TopicId",
       t."DisplayName"
FROM audit."RecentEvent" e
         JOIN audit."EventKind" k ON k."Id" = e."KindId"
         LEFT JOIN audit."Topic" t ON t."Id" = e."TopicId";

ALTER TABLE audit."RecentEventView"
    OWNER to test;

CREATE MATERIALIZED VIEW audit."ArchivedEdgeView"
AS
WITH RECURSIVE edge
                   (
                    "EventId",
                    "LeftTopicId",
                    "LeftTopic",
                    "LeftTopicKindId",
                    "LeftTopicKind",
                    "AssociationId",
                    "Association",
                    "RightTopicId",
                    "RightTopic",
                    "RightTopicKindId",
                    "RightTopicKind"
                       )
                   AS
                   (SELECT e."Id",
                           a."Topic1",
                           t."DisplayName",
                           t."KindId",
                           k_1."Name",
                           a."AssociationId",
                           assoc."Name",
                           a."Topic2",
                           t_1."DisplayName",
                           t_1."KindId",
                           k_2."Name"
                    FROM audit."ArchivedEvent" e
                             INNER JOIN audit."ArchivedTopicAssociation" a ON a."Topic1" = e."TopicId"
                             INNER JOIN audit."Topic" t ON a."Topic1" = t."Id"
                             INNER JOIN audit."TopicKind" k_1 ON t."KindId" = k_1."Id"
                             INNER JOIN audit."Association" assoc ON a."AssociationId" = assoc."Id"
                             INNER JOIN audit."Topic" t_1 ON a."Topic2" = t_1."Id"
                             INNER JOIN audit."TopicKind" k_2 ON t_1."KindId" = k_2."Id"
                    UNION ALL
                    SELECT e_1."EventId",
                           e_1."RightTopicId",
                           e_1."RightTopic",
                           e_1."RightTopicKindId",
                           e_1."RightTopicKind",
                           ta."AssociationId",
                           assoc_1."Name",
                           ta."Topic2",
                           t_2."DisplayName",
                           t_2."KindId",
                           k_3."Name"
                    FROM audit."ArchivedTopicAssociation" ta
                             INNER JOIN audit."Association" assoc_1 ON ta."AssociationId" = assoc_1."Id"
                             INNER JOIN audit."Topic" t_2 ON ta."Topic2" = t_2."Id"
                             INNER JOIN audit."TopicKind" k_3 ON t_2."KindId" = k_3."Id"
                             INNER JOIN edge e_1 ON e_1."RightTopicId" = ta."Topic1")
select *
from edge;

CREATE UNIQUE INDEX "ArchivedEdgeView_Uidx" ON audit."ArchivedEdgeView" ("EventId", "LeftTopicId", "Association", "RightTopicId");

ALTER TABLE audit."ArchivedEdgeView"
    OWNER to test;

CREATE MATERIALIZED VIEW audit."ArchivedEventView"
AS
SELECT e."Id",
       e."KindId",
       k."Name" AS "Kind",
       e."CreatedAt",
       e."Severity",
       e."TopicId",
       t."DisplayName"
FROM audit."ArchivedEvent" e
         JOIN audit."EventKind" k ON k."Id" = e."KindId"
         LEFT JOIN audit."Topic" t ON t."Id" = e."TopicId";


CREATE UNIQUE INDEX "ArchivedEventView_Uidx" ON audit."ArchivedEventView" ("KindId");
CREATE INDEX "ArchivedEventView_KindId_Idx" ON audit."ArchivedEventView" ("KindId");
CREATE INDEX "ArchivedEventView_TopicId_Idx" ON audit."ArchivedEventView" ("TopicId");
CREATE INDEX "ArchivedEventView_Severity_Idx" ON audit."ArchivedEventView" ("Severity");
CREATE INDEX "ArchivedEventView_CreatedAt_Idx" ON audit."ArchivedEventView" ("CreatedAt" DESC);

ALTER TABLE audit."ArchivedEventView"
    OWNER to test;