The default PostgreSQL connection string ("User ID=test;Host=localhost;Port=5432;Database=test;")
can be overwritten by the environment variable PG_TEST_CONNECTIONSTRING.

The schema of the required test database can be found in the script "pg_schema.sql".

smtp4dev (https://github.com/rnwood/smtp4dev) is used for SMTP testing. Change the SMTP4DEV_EXE
environment variable to override the path. Disable smtp4dev by setting SMTP4DEV_DISABLED=1. The
HTTP url can be overriden by SMTP4DEV_URL.

SMTP server and details can be overriden by the following variables:

* SMTP_TEST_USERNAME
* SMTP_TEST_PASSWORD
* SMTP_TEST_HOST
* SMTP_TEST_PORT
* SMTP_TEST_STARTTLS_PORT
* SMTP_TEST_TLS_PORT