FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env

WORKDIR /app
COPY ./src ./

RUN dotnet restore ./Budget.sln

RUN dotnet build VolleyList.sln -c Release
        
WORKDIR /app/VolleyList.WebApi

RUN dotnet publish -c Release -o /app/out /p:Version=$VERSION  

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0

#ARG PROJECT
#ARG VERSION

ENV VERSION ${VERSION}
ENV ASPNETCORE_ENVIRONMENT=Stable
LABEL version=${VERSION}

ENV ASPNETCORE_URLS=http://*:80

# libc-dev is required by gRPC, which is required by openTelemetry lib
RUN #apt-get update && apt-get install -y libc-dev wget \
#  && rm -rf /var/lib/apt/lists/* 

WORKDIR /app
COPY --from=build-env /app/out .

EXPOSE 80
ENTRYPOINT ["dotnet", "VolleyList.WebApi.dll"]