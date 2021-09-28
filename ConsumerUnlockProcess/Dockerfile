FROM dperson/torproxy as base
ARG BookUrl

USER root

RUN apk add bash icu-libs krb5-libs libgcc libintl libssl1.1 libstdc++ zlib wget
RUN apk add libgdiplus --repository https://dl-3.alpinelinux.org/alpine/edge/testing/

RUN mkdir -p /usr/share/dotnet \
    && ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet 

RUN wget https://dot.net/v1/dotnet-install.sh
RUN chmod +x dotnet-install.sh
RUN ./dotnet-install.sh -c 3.1 --install-dir /usr/share/dotnet
RUN ./dotnet-install.sh -c 5.0 --install-dir /usr/share/dotnet

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["ConsumerUnlockProcess/ConsumerUnlockProcess.csproj", "ConsumerUnlockProcess/"]
COPY ["THttpWebRequest/THttpWebRequest.csproj", "THttpWebRequest/"]
RUN dotnet restore "ConsumerUnlockProcess/ConsumerUnlockProcess.csproj"

COPY ConsumerUnlockProcess ./ConsumerUnlockProcess
COPY DownloadManningBook ./DownloadManningBook
COPY THttpWebRequest ./THttpWebRequest

WORKDIR "/src/ConsumerUnlockProcess"
RUN dotnet build "ConsumerUnlockProcess.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet build "ConsumerUnlockProcess.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app/output
COPY --from=publish /app/publish .
RUN dotnet --version
COPY run.sh ./run.sh
RUN chmod 0755 run.sh

#for remote deubug c
RUN apk add --no-cache openssh-server unzip curl

ENTRYPOINT ["/sbin/tini", "--", "/app/output/run.sh"]
