#!/usr/bin/env bash

if [[ -z "${BOOK_URL}" ]]; then
	echo "Please input the BOOK_URL"
else
	bash /usr/bin/torproxy.sh & while ! curl --proxy http://localhost:8118 --output /dev/null --silent --head --fail http://google.com ; do sleep 1 ; done && dotnet /app/output/ConsumerUnlockProcess.dll http://localhost:8118
fi