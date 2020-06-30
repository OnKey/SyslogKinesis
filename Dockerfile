FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY SyslogKinesis/*.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY SyslogKinesis/* ./
RUN dotnet publish -c Release -o out SyslogKinesis.csproj

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/runtime:3.1
WORKDIR /syslogkinesis
COPY --from=build-env /app/out .
EXPOSE 514/udp
EXPOSE 514/tcp
ENTRYPOINT ["dotnet", "SyslogKinesis.dll"]