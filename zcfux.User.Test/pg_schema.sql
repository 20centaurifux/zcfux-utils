CREATE SCHEMA "user";

ALTER
    SCHEMA "user" OWNER to test;

CREATE TABLE "user"."Site"
(
    "Guid" uuid                  NOT NULL,
    "Name" character varying(30) NOT NULL
);

ALTER TABLE "user"."Site"
    OWNER TO test;

ALTER TABLE ONLY "user"."Site"
    ADD CONSTRAINT "Site_pkey" PRIMARY KEY ("Guid");

CREATE UNIQUE INDEX "Site_Name_Uidx" ON "user"."Site" ("Name");

CREATE TABLE "user"."Application"
(
    "Id"   int                   NOT NULL,
    "Name" character varying(30) NOT NULL
);

ALTER TABLE "user"."Application"
    OWNER TO test;

ALTER TABLE ONLY "user"."Application"
    ADD CONSTRAINT "Application_pkey" PRIMARY KEY ("Id");

CREATE UNIQUE INDEX "Application_Name_Uidx" ON "user"."Application" ("Name");

CREATE TABLE "user"."Origin"
(
    "Id"       int                   NOT NULL,
    "Name"     character varying(30) NOT NULL,
    "Writable" boolean default 't'   NOT NULL
);

ALTER TABLE "user"."Origin"
    OWNER TO test;

ALTER TABLE ONLY "user"."Origin"
    ADD CONSTRAINT "Origin_pkey" PRIMARY KEY ("Id");

CREATE UNIQUE INDEX "Origin_Name_Uidx" ON "user"."Origin" ("Name");

CREATE TABLE "user"."User"
(
    "Guid"      uuid                  NOT NULL,
    "Name"      character varying(30) NOT NULL,
    "OriginId"  int                   NOT NULL,
    "Status"    int DEFAULT 0         NOT NULL,
    "Firstname" character varying(50),
    "Lastname"  character varying(50)
);

ALTER TABLE "user"."User"
    OWNER TO test;

ALTER TABLE ONLY "user"."User"
    ADD CONSTRAINT "User_pkey" PRIMARY KEY ("Guid");

CREATE UNIQUE INDEX "User_Uidx" ON "user"."User" ("OriginId", "Name");

ALTER TABLE ONLY "user"."User"
    ADD CONSTRAINT "User_OriginId_fk"
        FOREIGN KEY ("OriginId")
            REFERENCES "user"."Origin" ("Id") ON
            DELETE
            CASCADE;

CREATE TABLE "user"."Password"
(
    "User"   uuid  NOT NULL,
    "Hash"   bytea NOT NULL,
    "Salt"   bytea NOT NULL,
    "Format" int   NOT NULL
);

ALTER TABLE "user"."Password"
    OWNER TO test;

ALTER TABLE ONLY "user"."Password"
    ADD CONSTRAINT "Password_pkey" PRIMARY KEY ("User");

ALTER TABLE ONLY "user"."Password"
    ADD CONSTRAINT "Password_User_fk"
        FOREIGN KEY ("User")
            REFERENCES "user"."User" ("Guid") ON
            DELETE
            CASCADE;

CREATE TABLE "user"."Group"
(
    "Guid" uuid                  NOT NULL,
    "Name" character varying(30) NOT NULL
);

ALTER TABLE "user"."Group"
    OWNER TO test;

ALTER TABLE ONLY "user"."Group"
    ADD CONSTRAINT "Group_pkey" PRIMARY KEY ("Guid");

CREATE UNIQUE INDEX "Group_Name_Uidx" ON "user"."Group" ("Name");

CREATE TABLE "user"."AssignedUser"
(
    "Site"  uuid NOT NULL,
    "Group" uuid NOT NULL,
    "User"  uuid NOT NULL
);

ALTER TABLE "user"."AssignedUser"
    OWNER TO test;

ALTER TABLE ONLY "user"."AssignedUser"
    ADD CONSTRAINT "AssignedUser_pkey" PRIMARY KEY ("Site", "Group", "User");

ALTER TABLE ONLY "user"."AssignedUser"
    ADD CONSTRAINT "AssignedUser_Site_fk"
        FOREIGN KEY ("Site")
            REFERENCES "user"."Site" ("Guid") ON
            DELETE
            CASCADE;

ALTER TABLE ONLY "user"."AssignedUser"
    ADD CONSTRAINT "AssignedUser_Group_fk"
        FOREIGN KEY ("Group")
            REFERENCES "user"."Group" ("Guid") ON
            DELETE
            CASCADE;

ALTER TABLE ONLY "user"."AssignedUser"
    ADD CONSTRAINT "AssignedUser_User_fk"
        FOREIGN KEY ("User")
            REFERENCES "user"."User" ("Guid") ON
            DELETE
            CASCADE;

CREATE TABLE "user"."PermissionCategory"
(
    "Id"            int                   NOT NULL,
    "Name"          character varying(30) NOT NULL,
    "ApplicationId" int                   NOT NULL
);

ALTER TABLE "user"."PermissionCategory"
    OWNER TO test;

ALTER TABLE ONLY "user"."PermissionCategory"
    ADD CONSTRAINT "PermissionCategory_pkey" PRIMARY KEY ("Id");

CREATE UNIQUE INDEX "Site_ApplicationIdName_Uidx" ON "user"."PermissionCategory" ("ApplicationId", "Name");

ALTER TABLE ONLY "user"."PermissionCategory"
    ADD CONSTRAINT "PermissionCategory_Application_fk"
        FOREIGN KEY ("ApplicationId")
            REFERENCES "user"."Application" ("Id") ON
            DELETE
            CASCADE;

CREATE TABLE "user"."Permission"
(
    "Id"         int                   NOT NULL,
    "CategoryId" int,
    "Name"       character varying(30) NOT NULL
);

ALTER TABLE "user"."Permission"
    OWNER TO test;

ALTER TABLE ONLY "user"."Permission"
    ADD CONSTRAINT "Permission_pkey" PRIMARY KEY ("Id");

CREATE UNIQUE INDEX "Permission_CategoryIdName_Uidx" ON "user"."Permission" ("CategoryId", "Name");

ALTER TABLE ONLY "user"."Permission"
    ADD CONSTRAINT "Permission_CategoryId_fk"
        FOREIGN KEY ("CategoryId")
            REFERENCES "user"."PermissionCategory" ("Id") ON
            DELETE
            CASCADE;

CREATE TABLE "user"."PermissionDependency"
(
    "PermissionId" int NOT NULL,
    "DependencyId" int NOT NULL
);

ALTER TABLE "user"."PermissionDependency"
    OWNER TO test;

ALTER TABLE ONLY "user"."PermissionDependency"
    ADD CONSTRAINT "PermissionDependency_pkey" PRIMARY KEY ("DependencyId", "PermissionId");

ALTER TABLE ONLY "user"."PermissionDependency"
    ADD CONSTRAINT "PermissionDependency_PreconditionId_fk"
        FOREIGN KEY ("DependencyId")
            REFERENCES "user"."Permission" ("Id") ON
            DELETE
            CASCADE;

ALTER TABLE ONLY "user"."PermissionDependency"
    ADD CONSTRAINT "RequiredPermission_Permission_fk"
        FOREIGN KEY ("PermissionId")
            REFERENCES "user"."Permission" ("Id") ON
            DELETE
            CASCADE;

CREATE TABLE "user"."GrantedPermission"
(
    "Group"        uuid NOT NULL,
    "PermissionId" int  NOT NULL
);

ALTER TABLE "user"."GrantedPermission"
    OWNER TO test;

ALTER TABLE ONLY "user"."GrantedPermission"
    ADD CONSTRAINT "GrantedPermission_pkey" PRIMARY KEY ("Group", "PermissionId");

ALTER TABLE ONLY "user"."GrantedPermission"
    ADD CONSTRAINT "GrantedPermission_Group_fk"
        FOREIGN KEY ("Group")
            REFERENCES "user"."Group" ("Guid") ON
            DELETE
            CASCADE;

ALTER TABLE ONLY "user"."GrantedPermission"
    ADD CONSTRAINT "GrantedPermission_PermissionId_fk"
        FOREIGN KEY ("PermissionId")
            REFERENCES "user"."Permission" ("Id") ON
            DELETE
            CASCADE;

CREATE OR REPLACE VIEW "user"."Users"
AS
SELECT u."Guid",
       o."Id"       AS "OriginId",
       o."Name"     AS "Origin",
       u."Name",
       u."Firstname",
       u."Lastname",
       o."Writable" AS "Writable",
       u."Status"
FROM "user"."User" u
         JOIN "user"."Origin" o ON u."OriginId" = o."Id";

ALTER
    VIEW "user"."Users"
    OWNER TO test;

CREATE OR REPLACE VIEW "user"."AssignedUsers"
AS
SELECT s."Guid" "SiteUid",
       s."Name" "Site",
       o."Id"   "OriginId",
       o."Name" "Origin",
       o."Writable",
       u."Guid" "UserUid",
       u."Name" "Username",
       u."Status",
       u."Firstname",
       u."Lastname",
       g."Guid" "GroupUid",
       g."Name" "Group"
FROM "user"."AssignedUser" a
         INNER JOIN "user"."User" u on a."User" = u."Guid"
         INNER JOIN "user"."Origin" o on u."OriginId" = o."Id"
         INNER JOIN "user"."Group" g on a."Group" = g."Guid"
         INNER JOIN "user"."Site" s on a."Site" = s."Guid";

ALTER
    VIEW "user"."AssignedUsers"
    OWNER TO test;

CREATE OR REPLACE VIEW "user"."PermissionCategories"
AS
SELECT a."Id"   "ApplicationId",
       a."Name" "Application",
       c."Id",
       c."Name"
FROM "user"."PermissionCategory" c
         INNER JOIN "user"."Application" a on c."ApplicationId" = a."Id";

ALTER
    VIEW "user"."PermissionCategories"
    OWNER TO test;

CREATE OR REPLACE VIEW "user"."Permissions"
AS
SELECT a."Id"   "ApplicationId",
       a."Name" "Application",
       c."Id"   "CategoryId",
       c."Name" "Category",
       p."Id",
       p."Name"
FROM "user"."Permission" p
         INNER JOIN "user"."PermissionCategory" c ON p."CategoryId" = c."Id"
         INNER JOIN "user"."Application" a on c."ApplicationId" = a."Id";

ALTER
    VIEW "user"."Permissions"
    OWNER TO test;


CREATE OR REPLACE VIEW "user"."PermissionDependencies"
AS
SELECT a1."Id"   "ApplicationId",
       a1."Name" "Application",
       c1."Id"   "CategoryId",
       c1."Name" "Category",
       p1."Id"   "PermissionId",
       p1."Name" "Permission",
       a2."Id"   "Dep_ApplicationId",
       a2."Name" "Dep_Application",
       c2."Id"   "Dep_CategoryId",
       c2."Name" "Dep_Category",
       p2."Id"   "Dep_PermissionId",
       p2."Name" "Dep_Permission"
FROM "user"."PermissionDependency" d
         INNER JOIN "user"."Permission" p1 ON p1."Id" = d."PermissionId"
         INNER JOIN "user"."PermissionCategory" c1 ON p1."CategoryId" = c1."Id"
         INNER JOIN "user"."Application" a1 on c1."ApplicationId" = a1."Id"
         INNER JOIN "user"."Permission" p2 ON p2."Id" = d."DependencyId"
         INNER JOIN "user"."PermissionCategory" c2 ON p2."CategoryId" = c2."Id"
         INNER JOIN "user"."Application" a2 on c2."ApplicationId" = a2."Id";

ALTER
    VIEW "user"."PermissionDependencies"
    OWNER TO test;

CREATE OR REPLACE VIEW "user"."GrantedPermissions"
AS

SELECT grp."Guid" "GroupUid",
       grp."Name" "Group",
       p."Id"     "Id",
       p."Name"   "Name",
       c."Id"     "CategoryId",
       c."Name"   "Category",
       a."Id"     "ApplicationId",
       a."Name"   "Application"
FROM "user"."GrantedPermission" g
         INNER JOIN "user"."Group" grp ON g."Group" = grp."Guid"
         INNER JOIN "user"."Permission" p ON g."PermissionId" = p."Id"
         INNER JOIN "user"."PermissionCategory" c ON p."CategoryId" = c."Id"
         INNER JOIN "user"."Application" a ON c."ApplicationId" = a."Id";

ALTER
    VIEW "user"."GrantedPermissions"
    OWNER TO test;