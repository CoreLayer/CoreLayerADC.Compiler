FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
COPY . /app
WORKDIR /app

#--configfile NuGet.Config
RUN dotnet restore 
RUN dotnet publish -c Release -o out -r linux-x64 --self-contained

# Build runtime image
#FROM mcr.microsoft.com/dotnet/core/runtime:3.1
FROM ubuntu:latest
WORKDIR /app
COPY --from=build-env /app/out .
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

ENTRYPOINT ["/app/CoreLayerADC.Compiler"]