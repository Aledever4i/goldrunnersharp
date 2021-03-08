FROM mcr.microsoft.com/dotnet/aspnet:5.0

WORKDIR /app
COPY . /app

RUN nuget.exe restore goldrunnersharp.csproj -SolutionDirectory ../ -Verbosity normal
RUN MSBuild.exe goldrunnersharp.csproj /t:build /p:Configuration=Release /p:OutputPath=./out

WORKDIR /app/out

ENTRYPOINT ["goldrunnersharp.exe"]