FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "goldrunnersharp.dll"]


#FROM mcr.microsoft.com/dotnet/aspnet:5.0
#
#WORKDIR /app
#COPY . /app
#
#RUN nuget.exe restore goldrunnersharp.csproj -SolutionDirectory ../ -Verbosity normal
#RUN MSBuild.exe goldrunnersharp.csproj /t:build /p:Configuration=Release /p:OutputPath=./out
#
#WORKDIR /app/out
#
#ENTRYPOINT ["goldrunnersharp.exe"]