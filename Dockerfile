FROM dperson/torproxy
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

WORKDIR /app
COPY DownloadManningBook/DownloadManningBook.csproj ./DownloadManningBook/DownloadManningBook.csproj
COPY THttpWebRequest/THttpWebRequest.csproj ./THttpWebRequest/THttpWebRequest.csproj
COPY DownloadManningBook.sln ./DownloadManningBook.sln
RUN dotnet restore 

COPY DownloadManningBook ./DownloadManningBook
COPY THttpWebRequest ./THttpWebRequest
RUN dotnet build -o output

WORKDIR /app/output

COPY run.sh ./run.sh
RUN chmod 0755 run.sh

RUN apk add --no-cache openssh-server unzip curl

ENTRYPOINT ["/sbin/tini", "--", "/app/output/run.sh"]
