#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["DownloadManningBook/DownloadManningBook.csproj", "DownloadManningBook/"]
COPY ["THttpWebRequest/THttpWebRequest.csproj", "THttpWebRequest/"]
RUN dotnet restore "DownloadManningBook/DownloadManningBook.csproj"

COPY DownloadManningBook ./DownloadManningBook
COPY THttpWebRequest ./THttpWebRequest

WORKDIR "/src/DownloadManningBook"
RUN dotnet build "DownloadManningBook.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DownloadManningBook.csproj" -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN apt-get update && apt-get install -y openssh-server unzip curl

ENTRYPOINT ["dotnet", "DownloadManningBook.dll"]