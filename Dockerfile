FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /source

COPY *.sln .
COPY *.csproj ./goldrunnersharp/
RUN dotnet restore

FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /app
COPY --from=build /app ./
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