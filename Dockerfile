FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env

WORKDIR /app
COPY ./src ./


RUN ls
RUN dotnet restore ./VolleyList.sln

RUN dotnet build VolleyList.sln -c Release
        
WORKDIR /app/VolleyList.WebApi

RUN dotnet publish -c Release -o /app/out 

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0

#ARG PROJECT
#ARG VERSION

ENV VERSION ${VERSION}
ENV ASPNETCORE_ENVIRONMENT=Stable
LABEL version=${VERSION}

ENV ASPNETCORE_URLS=http://*:8080


WORKDIR /app
COPY --from=build-env /app/out .

EXPOSE 8080
ENTRYPOINT ["dotnet", "VolleyList.WebApi.dll"]