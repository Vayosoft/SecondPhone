##https://learn.microsoft.com/ru-ru/ef/core/cli/dotnet
##https://learn.microsoft.com/ru-ru/ef/core/managing-schemas/migrations/applying?tabs=dotnet-core-cli
dotnet add package Microsoft.EntityFrameworkCore.Design

dotnet tool install --global dotnet-ef


dotnet ef migrations list

## To delete an existing database 
dotnet ef database update 0 --context HubDbContext 

dotnet ef database drop -f -v

## To remove existing migrations 
dotnet ef migrations remove --context HubDbContext 

## To create a new EF migration, with name InitialCreate
dotnet ef migrations add InitialCreate --context HubDbContext -- --arg1 val1

## To apply migrations to the database
## All tables specified in SampleDbContext would be created in the database
dotnet ef database update --context SampleDbContext
dotnet ef database update InitialCreate
dotnet ef database update 20180904195021_InitialCreate --connection your_connection_string

## To generate all the scripts
## From Blank Database Till Latest Migration Created
dotnet ef migrations script --output all_scripts.sql
dotnet ef dbcontext script

## Command to Specify FROM Migration Name
## LastMigrationApplied is the name of migration that has already been applied to database
## The scripts include all the changes from latest migration generated
dotnet ef migrations script LastMigrationApplied  --output scripts.sql

## Command to Specify FROM and TO migration names
## LastMigrationApplied is the name of migration that has already been applied to database
## AnyOtherMigration would become the last migration applied to the database after running this command
dotnet ef migrations script LastMigrationApplied AnyOtherMigration --output scripts.sql

## To generate all the scripts
## From Blank Database Till Latest Migration Created
## --idempotent option is to generate idempotent SQL scripts
dotnet ef migrations script --output all_scripts.sql --idempotent


dotnet ef migrations bundle --self-contained -r linux-x64
.\efbundle.exe --connection 'Data Source=(local)\MSSQLSERVER;Initial Catalog=Blogging;User ID=myUsername;Password=myPassword'
