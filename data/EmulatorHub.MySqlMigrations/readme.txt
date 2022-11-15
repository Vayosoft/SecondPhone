dotnet add package Microsoft.EntityFrameworkCore.Design

dotnet tool install --global dotnet-ef

dotnet ef migrations add Initial 

dotnet ef database update
...
dotnet ef migrations list
dotnet ef migrations remove 