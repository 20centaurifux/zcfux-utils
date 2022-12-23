CREATE SCHEMA translation;
ALTER SCHEMA translation OWNER to test;
CREATE TABLE translation."TextCategory" (
  "Id" int NOT NULL,
  "Name" character varying(50) NOT NULL
);
ALTER TABLE
  translation."TextCategory" OWNER to test;
ALTER TABLE
  ONLY translation."TextCategory"
ADD
  CONSTRAINT "TextCategory_pkey" PRIMARY KEY ("Id");
ALTER TABLE
  ONLY translation."TextCategory"
ADD
  CONSTRAINT unique_translation_category_name UNIQUE ("Name");
CREATE SEQUENCE translation.text_resource_id_seq START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
ALTER SEQUENCE translation.text_resource_id_seq OWNER to test;
CREATE TABLE translation."TextResource" (
    "Id" int DEFAULT nextval('translation.text_resource_id_seq' :: regclass) NOT NULL,
    "CategoryId" int NOT NULL,
    "MsgId" character varying(255) NOT NULL
  );
ALTER TABLE
  translation."TextResource" OWNER to test;
ALTER TABLE
  ONLY translation."TextResource"
ADD
  CONSTRAINT "TextResource_pkey" PRIMARY KEY ("Id");
ALTER TABLE
  ONLY translation."TextResource"
ADD
  CONSTRAINT unique_text_resource_category_name UNIQUE ("CategoryId", "MsgId");
ALTER TABLE
  ONLY translation."TextResource"
ADD
  CONSTRAINT "TextResource_CategoryId_fk" FOREIGN KEY ("CategoryId") REFERENCES translation."TextCategory" ("Id") ON DELETE CASCADE;
CREATE TABLE translation."Locale" (
    "Id" int NOT NULL,
    "Name" character varying(50) NOT NULL
  );
ALTER TABLE
  translation."Locale" OWNER to test;
ALTER TABLE
  ONLY translation."Locale"
ADD
  CONSTRAINT "Locale_pkey" PRIMARY KEY ("Id");
ALTER TABLE
  ONLY translation."Locale"
ADD
  CONSTRAINT unique_locale_name UNIQUE ("Name");
CREATE TABLE translation."TranslatedText" (
    "ResourceId" int NOT NULL,
    "LocaleId" integer NOT NULL,
    "Translation" character varying(255) NOT NULL
  );
ALTER TABLE
  translation."TranslatedText" OWNER to test;
ALTER TABLE
  ONLY translation."TranslatedText"
ADD
  CONSTRAINT "TranslatedText_pkey" PRIMARY KEY ("ResourceId", "LocaleId");
ALTER TABLE
  ONLY translation."TranslatedText"
ADD
  CONSTRAINT "TranslatedText_ResourceId_fk" FOREIGN KEY ("ResourceId") REFERENCES translation."TextResource" ("Id") ON DELETE CASCADE;
ALTER TABLE
  ONLY translation."TranslatedText"
ADD
  CONSTRAINT "TranslatedText_LocaleId_fk" FOREIGN KEY ("LocaleId") REFERENCES translation."Locale" ("Id") ON DELETE CASCADE;
CREATE SCHEMA audit;
ALTER SCHEMA audit OWNER to test;
CREATE TABLE audit."TopicKind" (
    "Id" int NOT NULL,
    "Name" character varying(30) NOT NULL
  );
ALTER TABLE
  audit."TopicKind" OWNER to test;
ALTER TABLE
  ONLY audit."TopicKind"
ADD
  CONSTRAINT "TopicKind_pkey" PRIMARY KEY ("Id");
ALTER TABLE
  ONLY audit."TopicKind"
ADD
  CONSTRAINT unique_topic_kind_name UNIQUE ("Name");
CREATE SEQUENCE audit.topic_id_seq START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
ALTER SEQUENCE audit.topic_id_seq OWNER to test;
CREATE TABLE audit."Topic" (
    "Id" bigint DEFAULT nextval('audit.topic_id_seq' :: regclass) NOT NULL,
    "KindId" int NOT NULL,
    "TextId" int NOT NULL,
    "Translatable" boolean DEFAULT ('F') NOT NULL
  );
ALTER TABLE
  audit."Topic" OWNER to test;
ALTER TABLE
  ONLY audit."Topic"
ADD
  CONSTRAINT "Topic_pkey" PRIMARY KEY ("Id");
ALTER TABLE
  ONLY audit."Topic"
ADD
  CONSTRAINT "Topic_KindId_fk" FOREIGN KEY ("KindId") REFERENCES audit."TopicKind" ("Id");
ALTER TABLE
  audit."Topic"
ADD
  CONSTRAINT "Topic_TextId_fk" FOREIGN KEY ("TextId") REFERENCES translation."TextResource" ("Id");
CREATE INDEX "Topic_KindId_Idx" ON audit."Topic" ("KindId");
CREATE
  OR REPLACE VIEW audit."TopicView" AS
SELECT
  t."Id",
  k."Id" AS "KindId",
  k."Name" AS "Kind",
  tr."Id" AS "TextId",
  tr."MsgId" AS "MsgId",
  tr."CategoryId" AS "TextCategoryId",
  c."Name" AS "TextCategory",
  t."Translatable"
FROM
  audit."Topic" t
  JOIN audit."TopicKind" k ON k."Id" = t."KindId"
  JOIN translation."TextResource" tr ON tr."Id" = "TextId"
  JOIN translation."TextCategory" c ON c."Id" = "CategoryId";
ALTER TABLE
  audit."TopicView" OWNER to test;
CREATE TABLE audit."Association" (
    "Id" integer NOT NULL,
    "Name" character varying(30) NOT NULL
  );
ALTER TABLE
  audit."Association" OWNER to test;
ALTER TABLE
  ONLY audit."Association"
ADD
  CONSTRAINT "Association_pkey" PRIMARY KEY ("Id");
CREATE SEQUENCE audit.topic_association_id_seq START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
ALTER SEQUENCE audit."topic_association_id_seq" OWNER to test;
CREATE TABLE audit."TopicAssociation" (
    "Id" bigint DEFAULT nextval('audit.topic_association_id_seq' :: regclass) NOT NULL,
    "Archived" boolean DEFAULT ('F') NOT NULL,
    "Topic1" bigint NOT NULL,
    "AssociationId" integer NOT NULL,
    "Topic2" bigint NOT NULL,
    PRIMARY KEY ("Id", "Archived")
  ) PARTITION BY LIST ("Archived");
ALTER TABLE
  audit."TopicAssociation" OWNER to test;
CREATE INDEX "TopicAssociation_Topic1_Idx" ON audit."TopicAssociation" ("Topic1");
CREATE INDEX "TopicAssociation_AssociationId_Idx" ON audit."TopicAssociation" ("AssociationId");
CREATE INDEX "TopicAssociation_Topic2_Idx" ON audit."TopicAssociation" ("Topic2");
ALTER TABLE
  audit."TopicAssociation"
ADD
  CONSTRAINT "TopicAssociation_Topic1_fk" FOREIGN KEY ("Topic1") REFERENCES audit."Topic" ("Id");
ALTER TABLE
  audit."TopicAssociation"
ADD
  CONSTRAINT "TopicAssociation_AssociationId_fk" FOREIGN KEY ("AssociationId") REFERENCES audit."Association" ("Id");
ALTER TABLE
  audit."TopicAssociation"
ADD
  CONSTRAINT "TopicAssociation_Topic2_fk" FOREIGN KEY ("Topic2") REFERENCES audit."Topic" ("Id");
CREATE TABLE audit."RecentTopicAssociation" PARTITION OF audit."TopicAssociation" FOR
VALUES
  IN ('F');
ALTER TABLE
  audit."RecentTopicAssociation" OWNER to test;
CREATE TABLE audit."ArchivedTopicAssociation" PARTITION OF audit."TopicAssociation" FOR
VALUES
  IN ('T');
ALTER TABLE
  audit."ArchivedTopicAssociation" OWNER to test;
CREATE TABLE "audit"."EventKind" (
    "Id" int NOT NULL,
    "Name" character varying(30) NOT NULL,
    PRIMARY KEY ("Id")
  );
ALTER TABLE
  audit."EventKind" OWNER to test;
CREATE SEQUENCE audit.event_id_seq START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
ALTER SEQUENCE audit.event_id_seq OWNER to test;
CREATE TABLE audit."Event" (
    "Id" bigint DEFAULT nextval('audit.event_id_seq' :: regclass) NOT NULL,
    "KindId" int NOT NULL,
    "Severity" smallint NOT NULL,
    "Archived" boolean DEFAULT ('F') NOT NULL,
    "CreatedAt" timestamp DEFAULT NOW() NOT NULL,
    "TopicId" bigint,
    PRIMARY KEY ("Id", "Archived")
  ) PARTITION BY LIST ("Archived");
ALTER TABLE
  audit."Event" OWNER to test;
CREATE INDEX "Event_KindId_Idx" ON audit."Event" ("KindId");
CREATE INDEX "Event_TopicId_Idx" ON audit."Event" ("TopicId");
CREATE INDEX "Event_Severity_Idx" ON audit."Event" ("Severity");
CREATE INDEX "Event_CreatedAt_Idx" ON audit."Event" ("CreatedAt" DESC);
ALTER TABLE
  audit."Event"
ADD
  CONSTRAINT "Event_KindId_fk" FOREIGN KEY ("KindId") REFERENCES audit."EventKind" ("Id");
ALTER TABLE
  audit."Event"
ADD
  CONSTRAINT "Event_TopicId_fk" FOREIGN KEY ("TopicId") REFERENCES audit."Topic" ("Id");
CREATE TABLE audit."RecentEvent" PARTITION OF audit."Event" FOR
VALUES
  IN ('F');
ALTER TABLE
  audit."RecentEvent" OWNER to test;
CREATE TABLE audit."ArchivedEvent" PARTITION OF audit."Event" FOR
VALUES
  IN ('T');
ALTER TABLE
  audit."ArchivedEvent" OWNER to test;
CREATE
  OR REPLACE VIEW audit."RecentEventView" AS
SELECT
  e."Id",
  e."KindId",
  k."Name" AS "Kind",
  e."CreatedAt",
  e."Severity",
  e."TopicId",
  COALESCE(trans."Translation", res."MsgId") AS "DisplayName",
  trans."LocaleId" AS "LocaleId"
FROM
  audit."RecentEvent" e
  JOIN audit."EventKind" k ON k."Id" = e."KindId"
  LEFT JOIN audit."Topic" t ON t."Id" = e."TopicId"
  LEFT JOIN translation."TextResource" res ON res."Id" = "TextId"
  LEFT JOIN translation."TranslatedText" trans ON t."Translatable"
  AND trans."ResourceId" = "TextId";
ALTER TABLE
  audit."RecentEventView" OWNER to test;
CREATE MATERIALIZED VIEW audit."ArchivedEdgeView" AS WITH RECURSIVE edge (
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
    "RightTopicKind",
    "LocaleId"
  ) as (
    SELECT
      e."Id",
      a."Topic1",
      COALESCE(ltrans."Translation", lres."MsgId"),
      t1."KindId",
      lk."Name",
      a."AssociationId",
      assoc."Name",
      a."Topic2",
      rres."MsgId",
      t2."KindId",
      rk."Name",
      ltrans."LocaleId"
    FROM
      audit."ArchivedEvent" e
      INNER JOIN audit."ArchivedTopicAssociation" a ON a."Topic1" = e."TopicId"
      INNER JOIN audit."Topic" t1 ON a."Topic1" = t1."Id"
      INNER JOIN audit."TopicKind" lk ON t1."KindId" = lk."Id"
      INNER JOIN translation."TextResource" lres ON lres."Id" = t1."TextId"
      LEFT JOIN translation."TranslatedText" ltrans ON t1."Translatable"
      AND ltrans."ResourceId" = lres."Id"
      INNER JOIN audit."Association" assoc ON a."AssociationId" = assoc."Id"
      INNER JOIN audit."Topic" t2 ON a."Topic2" = t2."Id"
      INNER JOIN audit."TopicKind" rk ON t2."KindId" = rk."Id"
      INNER JOIN translation."TextResource" rres ON rres."Id" = t2."TextId"
      LEFT JOIN translation."TranslatedText" rtrans ON t2."Translatable"
      and rtrans."ResourceId" = rres."Id"
      and rtrans."LocaleId" = ltrans."LocaleId"
    UNION ALL
    SELECT
      e."EventId",
      e."RightTopicId",
      e."RightTopic",
      e."RightTopicKindId",
      e."RightTopicKind",
      ta."AssociationId",
      assoc."Name",
      t2."Id",
      COALESCE(rtrans."Translation", rres."MsgId"),
      t2."KindId",
      rk."Name",
      e."LocaleId"
    FROM
      audit."ArchivedTopicAssociation" ta
      INNER JOIN audit."Association" assoc ON ta."AssociationId" = assoc."Id"
      INNER JOIN audit."Topic" t2 ON ta."Topic2" = t2."Id"
      INNER JOIN audit."TopicKind" rk ON t2."KindId" = rk."Id"
      INNER JOIN edge e ON e."RightTopicId" = ta."Topic1"
      INNER JOIN translation."TextResource" rres ON rres."Id" = t2."TextId"
      LEFT JOIN translation."TranslatedText" rtrans ON t2."Translatable"
      and rtrans."ResourceId" = rres."Id"
      and rtrans."LocaleId" = e."LocaleId"
  )
select
  *
from
  edge;
ALTER TABLE
  audit."ArchivedEdgeView" OWNER to test;
CREATE UNIQUE INDEX "ArchivedEdgeView_Uidx" ON audit."ArchivedEdgeView" (
    "EventId",
    "LeftTopicId",
    "Association",
    "RightTopicId",
    "LocaleId"
  );
CREATE MATERIALIZED VIEW audit."ArchivedEventView" AS
SELECT
  e."Id",
  e."KindId",
  k."Name" AS "Kind",
  e."CreatedAt",
  e."Severity",
  e."TopicId",
  COALESCE(trans."Translation", res."MsgId") AS "DisplayName",
  trans."LocaleId" AS "LocaleId"
FROM
  audit."ArchivedEvent" e
  JOIN audit."EventKind" k ON k."Id" = e."KindId"
  LEFT JOIN audit."Topic" t ON t."Id" = e."TopicId"
  LEFT JOIN translation."TextResource" res ON res."Id" = "TextId"
  LEFT JOIN translation."TranslatedText" trans ON t."Translatable"
  AND trans."ResourceId" = "TextId";
ALTER TABLE
  audit."ArchivedEventView" OWNER to test;
CREATE UNIQUE INDEX "ArchivedEventView_Uidx" ON audit."ArchivedEventView" ("Id", "LocaleId");
CREATE INDEX "ArchivedEventView_KindId_Idx" ON audit."ArchivedEventView" ("KindId");
CREATE INDEX "ArchivedEventView_TopicId_Idx" ON audit."ArchivedEventView" ("TopicId");
CREATE INDEX "ArchivedEventView_Severity_Idx" ON audit."ArchivedEventView" ("Severity");
CREATE INDEX "ArchivedEventView_CreatedAt_Idx" ON audit."ArchivedEventView" ("CreatedAt" DESC);