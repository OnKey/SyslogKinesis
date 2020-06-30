FROM mcr.microsoft.com/dotnet/core/runtime:3.1
COPY syslogkinesis /syslogkinesis
EXPOSE 514/udp
EXPOSE 514/tcp
WORKDIR /syslogkinesis
ENTRYPOINT ["dotnet", "SyslogKinesis.dll"]