# KodtestNorionBank

cd CodeSolution

dotnet new sln -n CodeSolution

mkdir src
mkdir tests

dotnet new webapi -n CodeSolution.Api -o src/CodeSolution.Api
dotnet new classlib -n CodeSolution.Core -o src/CodeSolution.Core
dotnet new xunit -n CodeSolution.Tests -o tests/CodeSolution.Tests

dotnet sln add src/CodeSolution.Api/CodeSolution.Api.csproj
dotnet sln add src/CodeSolution.Core/CodeSolution.Core.csproj
dotnet sln add tests/CodeSolution.Tests/CodeSolution.Tests.csproj

dotnet add src/CodeSolution.Api/CodeSolution.Api.csproj reference src/CodeSolution.Core/CodeSolution.Core.csproj

dotnet add tests/CodeSolution.Tests/CodeSolution.Tests.csproj reference src/CodeSolution.Core/CodeSolution.Core.csproj# KodtestNorionBank
# CodeTestNorionBank
