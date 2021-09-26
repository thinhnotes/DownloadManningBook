#!/usr/bin/env bash

if [[ -z "${BOOK_URL}" ]]; then
	echo "Please input the BOOK_URL"
elif [[ -z "${REPLICA_COUNT}" ]]; then
	echo "Please input the BOOK_URL"
else
	bash /usr/bin/torproxy.sh & sleep 15 && dotnet /app/output/DownloadManningBook.dll $BOOK_URL $REPLICA_COUNT $PROXY
fi