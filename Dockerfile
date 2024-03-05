FROM mcr.microsoft.com/dotnet/sdk:8.0

ENV PATH="${PATH}:/root/.dotnet/tools"

RUN dotnet tool install --global dotnet-trace

COPY bin/Release/net8.0/linux-x64/publish/ /publish/

ENV ASPNETCORE_URLS=http://0.0.0.0:5000

ENTRYPOINT ["/publish/dotnet-trace-container"]
EXPOSE 5000
